using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

// Advances the redstone circuit one tick at a time using BFS flood-fill for signal propagation.
public class TickSimulator
{
    private readonly CircuitBoard _board;
    private readonly Queue<(Position pos, Direction outputDir, int ticksLeft)> _pendingRepeaters = new();

    public int TickCount { get; private set; }

    public TickSimulator(CircuitBoard board)
    {
        _board = board;
    }

    /// <summary>Advances the simulation by one full redstone tick.</summary>
    public void Tick()
    {
        TickCount++;

        AdvanceTickables();
        ResetAllPower();

        BfsFlood(CollectSources());

        ResolveTorches();
        BfsFlood(CollectTorchSources());

        BfsFlood(ResolveRepeaters());
        ResolveLamps();
    }

    /// <summary>Resets tick count and clears repeater delay queue.</summary>
    public void Reset()
    {
        TickCount = 0;
        _pendingRepeaters.Clear();
    }

    // Calls Tick() on any entity that has time-based internal state (e.g. buttons).
    private void AdvanceTickables()
    {
        foreach (var pos in _board.AllPositions())
            if (_board.GetEntity(pos) is ITickable t) t.Tick();
    }

    // Sets all IPowerable blocks back to 0 before recomputing from scratch.
    private void ResetAllPower()
    {
        foreach (var pos in _board.AllPositions())
            if (_board.GetEntity(pos) is IPowerable p) p.UpdatePower(SignalStrength.Min);
    }

    // Seeds the BFS with the 4 neighbors of every active non-torch source.
    // Sources themselves are not Wire/Lamp, so we flood from their neighbors at full power.
    private Queue<(Position, int)> CollectSources()
    {
        var queue = new Queue<(Position, int)>();
        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is (Lever or RedstoneBlock or Button) && e.IsPowering())
                EnqueueNeighbors(pos, 15, queue);
        }
        return queue;
    }

    // Seeds the BFS with neighbors of every currently-lit torch.
    private Queue<(Position, int)> CollectTorchSources()
    {
        var queue = new Queue<(Position, int)>();
        foreach (var pos in _board.AllPositions())
            if (_board.GetEntity(pos) is Torch t && t.IsOn)
                EnqueueNeighbors(pos, 15, queue);
        return queue;
    }

    // Enqueues all in-bounds neighbors of pos at the given power level.
    private void EnqueueNeighbors(Position pos, int power, Queue<(Position, int)> queue)
    {
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            var n = pos.Move(dir);
            if (_board.IsInBounds(n)) queue.Enqueue((n, power));
        }
    }

    // BFS: floods power through Wire cells only. Lamps are resolved in ResolveLamps().
    // No visited set — power strictly decreases, so the guard prevents infinite loops.
    private void BfsFlood(Queue<(Position pos, int power)> seeds)
    {
        while (seeds.Count > 0)
        {
            var (current, power) = seeds.Dequeue();
            if (!_board.IsInBounds(current) || power <= 0) continue;

            if (_board.GetEntity(current) is not Wire wire) continue;
            if (power <= wire.CurrentPower.Value) continue;

            wire.UpdatePower(new SignalStrength(power));

            foreach (Direction dir in Enum.GetValues<Direction>())
            {
                var next = current.Move(dir);
                if (_board.IsInBounds(next))
                    seeds.Enqueue((next, power - 1));
            }
        }
    }

    // Lamps light up based on the maximum power of their immediate neighbors.
    private void ResolveLamps()
    {
        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is not RedstoneLamp lamp) continue;
            lamp.UpdatePower(new SignalStrength(MaxNeighborPower(pos)));
        }
    }

    // Re-evaluates every torch based on the current power of its four neighbors.
    // Always called unconditionally; CollectTorchSources() then seeds the BFS from lit torches.
    // NOTE: ResetAllPower() calls UpdatePower(0) on torches, which sets _on=true (inversion of 0).
    // That intermediate state makes the old "anyChanged" guard always false, so we flood
    // unconditionally instead and let CollectTorchSources() filter to only lit torches.
    private void ResolveTorches()
    {
        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is not Torch torch) continue;
            int maxNeighborPower = MaxNeighborPower(pos);
            ((IPowerable)torch).UpdatePower(new SignalStrength(maxNeighborPower));
        }
    }

    // Ticks down the repeater delay queue, then schedules outputs for newly-powered repeaters.
    // Returns BFS seeds so the caller can flood the signal through wires beyond the output cell.
    private Queue<(Position, int)> ResolveRepeaters()
    {
        var next = new List<(Position, Direction, int)>();
        var floodSeeds = new Queue<(Position, int)>();

        while (_pendingRepeaters.Count > 0)
        {
            var (pos, dir, ticksLeft) = _pendingRepeaters.Dequeue();
            if (ticksLeft > 1)
            {
                next.Add((pos, dir, ticksLeft - 1));
            }
            else
            {
                // Delay elapsed — seed a BFS flood from the output cell at full power.
                // Using BFS (not a direct UpdatePower) ensures the signal propagates through
                // the entire wire chain beyond the repeater, not just the first adjacent cell.
                var outPos = pos.Move(dir);
                if (_board.IsInBounds(outPos))
                    floodSeeds.Enqueue((outPos, 15));
            }
        }

        // Read each repeater's input this tick and schedule a new output if powered.
        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is not Repeater rep) continue;

            var inputPos = pos.Move(Opposite(rep.Output));
            int inputPower = 0;

            if (_board.IsInBounds(inputPos))
            {
                var src = _board.GetEntity(inputPos);
                inputPower = src switch
                {
                    IPowerable pw              => pw.CurrentPower.Value,
                    Lever l when l.IsOn        => 15,
                    RedstoneBlock              => 15,
                    Button b when b.IsPowering() => 15,
                    _                          => 0
                };
            }

            ((IPowerable)rep).UpdatePower(new SignalStrength(inputPower));
            if (rep.IsPowering())
                next.Add((pos, rep.Output, rep.Delay));
        }

        foreach (var item in next)
            _pendingRepeaters.Enqueue(item);

        return floodSeeds;
    }

    // Returns the maximum power level among all four neighbors of a position.
    private int MaxNeighborPower(Position pos)
    {
        int max = 0;
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            var np = pos.Move(dir);
            if (!_board.IsInBounds(np)) continue;
            var e = _board.GetEntity(np);
            int p = e switch
            {
                IPowerable pw => pw.CurrentPower.Value,
                Lever l when l.IsOn => 15,
                RedstoneBlock => 15,
                Button b when b.IsPowering() => 15,
                _ => 0
            };
            if (p > max) max = p;
        }
        return max;
    }

    private static Direction Opposite(Direction d) => (Direction)(((int)d + 2) % 4);
}

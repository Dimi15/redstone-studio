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

        // First flood: permanent sources (levers, redstone blocks, buttons).
        var seeds = CollectSources();
        BfsFlood(seeds);

        // Torch pass: torches invert based on wire power, then re-flood if any changed.
        if (ResolveTorches())
            BfsFlood(CollectTorchSources());

        ResolveRepeaters();
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

    // Collects every active power source that isn't a torch (torches are handled separately).
    private Queue<(Position, int)> CollectSources()
    {
        var queue = new Queue<(Position, int)>();
        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is (Lever or RedstoneBlock or Button) && e.IsPowering())
                queue.Enqueue((pos, 15));
        }
        return queue;
    }

    // Collects all torches that are currently lit (used for the secondary flood pass).
    private Queue<(Position, int)> CollectTorchSources()
    {
        var queue = new Queue<(Position, int)>();
        foreach (var pos in _board.AllPositions())
            if (_board.GetEntity(pos) is Torch t && t.IsOn)
                queue.Enqueue((pos, 15));
        return queue;
    }

    // BFS: power spreads from seeds through Wire and RedstoneLamp cells.
    // Wires propagate further; lamps absorb power but are terminal nodes.
    private void BfsFlood(Queue<(Position pos, int power)> seeds)
    {
        var visited = new HashSet<Position>();

        while (seeds.Count > 0)
        {
            var (current, power) = seeds.Dequeue();
            if (!_board.IsInBounds(current) || visited.Contains(current)) continue;
            visited.Add(current);

            var entity = _board.GetEntity(current);

            if (entity is Wire wire)
            {
                // Wires keep the highest power they receive from any direction.
                if (power <= wire.CurrentPower.Value) continue;
                wire.UpdatePower(new SignalStrength(power));

                if (power > 0)
                    foreach (Direction dir in Enum.GetValues<Direction>())
                    {
                        var next = current.Move(dir);
                        if (_board.IsInBounds(next) && !visited.Contains(next))
                            seeds.Enqueue((next, power - 1));
                    }
            }
            else if (entity is RedstoneLamp lamp)
            {
                // Lamps light up when adjacent to powered wires but don't pass power on.
                if (power > lamp.CurrentPower.Value)
                    lamp.UpdatePower(new SignalStrength(power));
            }
        }
    }

    // Re-evaluates every torch based on the current power of its four neighbors.
    // Returns true if at least one torch changed state (triggers a re-flood).
    private bool ResolveTorches()
    {
        bool anyChanged = false;

        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is not Torch torch) continue;

            bool wasPowering = torch.IsPowering();
            int maxNeighborPower = MaxNeighborPower(pos);

            ((IPowerable)torch).UpdatePower(new SignalStrength(maxNeighborPower));

            if (torch.IsPowering() != wasPowering) anyChanged = true;
        }

        return anyChanged;
    }

    // Ticks down the repeater delay queue, then enqueues new outputs for powered repeaters.
    private void ResolveRepeaters()
    {
        var next = new List<(Position, Direction, int)>();

        while (_pendingRepeaters.Count > 0)
        {
            var (pos, dir, ticksLeft) = _pendingRepeaters.Dequeue();
            if (ticksLeft > 1)
            {
                next.Add((pos, dir, ticksLeft - 1));
            }
            else
            {
                // Delay elapsed — emit into the output neighbor.
                var outPos = pos.Move(dir);
                if (_board.IsInBounds(outPos))
                {
                    var outEntity = _board.GetEntity(outPos);
                    if (outEntity is Wire w) w.UpdatePower(new SignalStrength(15));
                    else if (outEntity is RedstoneLamp l) l.UpdatePower(new SignalStrength(15));
                }
            }
        }

        // Enqueue new repeater outputs based on what their input side received this tick.
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
                    IPowerable pw => pw.CurrentPower.Value,
                    Lever l when l.IsOn => 15,
                    RedstoneBlock => 15,
                    Button b when b.IsPowering() => 15,
                    _ => 0
                };
            }

            ((IPowerable)rep).UpdatePower(new SignalStrength(inputPower));
            if (rep.IsPowering())
                next.Add((pos, rep.Output, rep.Delay));
        }

        foreach (var item in next)
            _pendingRepeaters.Enqueue(item);
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

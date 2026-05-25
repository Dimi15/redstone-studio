using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

// Manages redstone ticks and repeater delays using a BFS flood-fill algorithm.
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

        ResetAllPower();

        var seeds = CollectSources();
        BfsFlood(seeds);

        // Run a second pass for torches — their state depends on wire power after the first flood.
        bool anyTorchChanged = ResolveTorches();
        if (anyTorchChanged)
        {
            // Re-flood so wires powered by newly-lit torches get updated.
            BfsFlood(CollectTorchSources());
        }

        ResolveRepeaters();
    }

    /// <summary>Resets tick count and clears any pending repeater outputs.</summary>
    public void Reset()
    {
        TickCount = 0;
        _pendingRepeaters.Clear();
    }

    // Step 1: set all powered blocks back to 0 so we can recompute from scratch.
    private void ResetAllPower()
    {
        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is IPowerable p)
                p.UpdatePower(SignalStrength.Min);
        }
    }

    // Step 2: seed the BFS with every active power source that isn't a torch
    // (torches are handled after wires settle).
    private Queue<(Position, int)> CollectSources()
    {
        var queue = new Queue<(Position, int)>();
        foreach (var pos in _board.AllPositions())
        {
            var entity = _board.GetEntity(pos);
            if (entity is Lever or RedstoneBlock && entity.IsPowering())
                queue.Enqueue((pos, 15));
        }
        return queue;
    }

    // Collect lit torches as BFS seeds (called after torch state is resolved).
    private Queue<(Position, int)> CollectTorchSources()
    {
        var queue = new Queue<(Position, int)>();
        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is Torch t && t.IsOn)
                queue.Enqueue((pos, 15));
        }
        return queue;
    }

    // Step 3: BFS flood — power spreads from sources through wire blocks.
    private void BfsFlood(Queue<(Position pos, int power)> seeds)
    {
        var visited = new HashSet<Position>();

        while (seeds.Count > 0)
        {
            var (current, power) = seeds.Dequeue();

            if (!_board.IsInBounds(current) || visited.Contains(current)) continue;
            visited.Add(current);

            var entity = _board.GetEntity(current);

            // Wires take the highest power level they receive.
            if (entity is Wire wire)
            {
                if (power > wire.CurrentPower.Value)
                {
                    wire.UpdatePower(new SignalStrength(power));
                    if (power > 0)
                    {
                        foreach (Direction dir in Enum.GetValues<Direction>())
                        {
                            var neighbor = current.Move(dir);
                            if (_board.IsInBounds(neighbor) && !visited.Contains(neighbor))
                                seeds.Enqueue((neighbor, power - 1));
                        }
                    }
                }
            }
        }
    }

    // Step 4: re-evaluate every torch based on what its neighbors are now doing.
    // Returns true if any torch changed its lit state.
    private bool ResolveTorches()
    {
        bool changed = false;

        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is not Torch torch) continue;

            bool wasPowering = torch.IsPowering();
            int maxNeighborPower = 0;

            foreach (Direction dir in Enum.GetValues<Direction>())
            {
                var neighborPos = pos.Move(dir);
                if (!_board.IsInBounds(neighborPos)) continue;

                var neighbor = _board.GetEntity(neighborPos);
                if (neighbor is IPowerable pw)
                    maxNeighborPower = Math.Max(maxNeighborPower, pw.CurrentPower.Value);
                else if (neighbor is Lever l && l.IsOn)
                    maxNeighborPower = 15;
                else if (neighbor is RedstoneBlock)
                    maxNeighborPower = 15;
            }

            ((IPowerable)torch).UpdatePower(new SignalStrength(maxNeighborPower));

            if (torch.IsPowering() != wasPowering) changed = true;
        }

        return changed;
    }

    // Step 5: tick down repeater delay queues and enqueue new repeater firings.
    private void ResolveRepeaters()
    {
        // Tick down existing pending outputs.
        var remaining = new Queue<(Position pos, Direction dir, int ticksLeft)>();
        while (_pendingRepeaters.Count > 0)
        {
            var (pos, dir, ticksLeft) = _pendingRepeaters.Dequeue();
            if (ticksLeft - 1 > 0)
            {
                remaining.Enqueue((pos, dir, ticksLeft - 1));
            }
            else
            {
                // Delay elapsed — emit power in the output direction.
                var outputPos = pos.Move(dir);
                if (_board.IsInBounds(outputPos) && _board.GetEntity(outputPos) is Wire outWire)
                    outWire.UpdatePower(new SignalStrength(15));
            }
        }

        // Check each repeater for new input.
        foreach (var pos in _board.AllPositions())
        {
            if (_board.GetEntity(pos) is not Repeater rep) continue;

            Direction inputDir = Opposite(rep.Output);
            var inputPos = pos.Move(inputDir);

            int inputPower = 0;
            if (_board.IsInBounds(inputPos))
            {
                var inputEntity = _board.GetEntity(inputPos);
                if (inputEntity is IPowerable pw) inputPower = pw.CurrentPower.Value;
                else if (inputEntity is Lever l && l.IsOn) inputPower = 15;
                else if (inputEntity is RedstoneBlock) inputPower = 15;
            }

            ((IPowerable)rep).UpdatePower(new SignalStrength(inputPower));

            if (rep.IsPowering())
                remaining.Enqueue((pos, rep.Output, rep.Delay));
        }

        // Refill the queue with updated state.
        while (remaining.Count > 0)
            _pendingRepeaters.Enqueue(remaining.Dequeue());
    }

    private static Direction Opposite(Direction d) =>
        (Direction)(((int)d + 2) % 4);
}

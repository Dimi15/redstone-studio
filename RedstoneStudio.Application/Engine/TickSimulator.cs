using System.Collections.Generic;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

public class TickSimulator
{
    private readonly CircuitBoard _board;

    // Each entry is a repeater counting down before it fires:
    // (position, output direction, ticks remaining).
    private readonly Queue<(Position pos, Direction outputDir, int ticksLeft)> _pendingRepeaters = new();

    // Blocks powered this tick by a fired repeater.
    // Populated in ResolveRepeaters(), cleared at the start of each tick.
    // Used so that lamps, torches, and other repeaters can detect a powered block.
    private readonly HashSet<Position> _poweredBlocks = new();

    public int TickCount { get; private set; }

    public TickSimulator(CircuitBoard board)
    {
        _board = board;
    }

    // Runs one full simulation step. The order of the phases matters.
    public void Tick()
    {
        TickCount++;
        AdvanceTickables();              // 1. button countdown
        ResetAllPower();                 // 2. wipe all power to 0 (also clears _poweredBlocks)
        BfsFlood(CollectSources());      // 3. flood from lever / button / redstone block
        ResolveTorches();               // 4. flip torches based on updated wire power
        BfsFlood(CollectTorchSources()); // 5. flood from lit torches
        ResolveRepeaters();             // 6. tick down delay queue, flood from fired repeaters
        ResolveLamps();                 // 7. light lamps based on neighbor power
    }

    // Resets the tick counter and empties the repeater delay queue.
    public void Reset()
    {
        TickCount = 0;
        _pendingRepeaters.Clear();
    }

    // --- Private phases ---

    // Calls Tick() on every ITickable entity (currently only Button).
    private void AdvanceTickables()
    {
        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is ITickable t)
            {
                t.Tick();
            }
        }
    }

    // Sets every IPowerable entity back to power 0 so the tick starts from a clean state.
    // Also clears the powered-blocks set from the previous tick.
    private void ResetAllPower()
    {
        _poweredBlocks.Clear();

        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is IPowerable p)
            {
                p.UpdatePower(SignalStrength.Min);
            }
        }
    }

    // Finds every active source (lever on, button pulsing, redstone block) and
    // enqueues its four neighbors at full power 15.
    // We enqueue neighbors — not the source itself — because the BFS only
    // propagates through Wire cells, and sources are not wires.
    private Queue<(Position, int)> CollectSources()
    {
        var queue = new Queue<(Position, int)>();

        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            bool isActiveSource = false;

            if (e is Lever && e.IsPowering())
            {
                isActiveSource = true;
            }
            else if (e is RedstoneBlock)
            {
                isActiveSource = true;
            }
            else if (e is Button && e.IsPowering())
            {
                isActiveSource = true;
            }

            if (isActiveSource)
            {
                EnqueueNeighbors(pos, 15, queue);
            }
        }

        return queue;
    }

    // Same as CollectSources but for torches: enqueues neighbors of every lit torch.
    private Queue<(Position, int)> CollectTorchSources()
    {
        var queue = new Queue<(Position, int)>();

        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is Torch torch && torch.IsOn)
            {
                EnqueueNeighbors(pos, 15, queue);
            }
        }

        return queue;
    }

    // Adds all four in-bounds neighbors of pos to the queue at the given power level.
    private void EnqueueNeighbors(Position pos, int power, Queue<(Position, int)> queue)
    {
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            var neighbor = pos.Move(dir);
            if (_board.IsInBounds(neighbor))
            {
                queue.Enqueue((neighbor, power));
            }
        }
    }

    // BFS flood: propagates power through Wire cells.
    //
    // How it works:
    //   - Each step dequeues a position and a power level.
    //   - If the cell is a Wire and the incoming power is higher than what it already
    //     holds, the wire gets updated and all four neighbors are enqueued at power - 1.
    //   - Non-Wire cells are simply skipped (they stop the signal).
    //
    // Why no visited set?
    //   Power decreases by 1 at every hop (15 → 14 → 13 → …). A wire is only updated
    //   when the new value is strictly greater than its current one. So once a wire
    //   reaches a given power, any later arrival with equal or lower power is ignored.
    //   This guarantees termination without a HashSet.
    //
    // Why we seed neighbors, not sources:
    //   Sources such as Lever and RedstoneBlock are not Wire cells. If we enqueued them
    //   directly, the BFS would dequeue them and skip immediately. Seeding their neighbors
    //   at power 15 means the BFS starts where the wire actually begins.
    private void BfsFlood(Queue<(Position pos, int power)> seeds)
    {
        while (seeds.Count > 0)
        {
            var (current, power) = seeds.Dequeue();

            if (!_board.IsInBounds(current) || power <= 0) continue;

            var entity = _board.GetEntity(current);
            if (entity is not Wire wire) continue;          // only Wire cells carry signal
            if (power <= wire.CurrentPower.Value) continue; // already at a higher or equal level

            wire.UpdatePower(new SignalStrength(power));

            // Spread to all four neighbors, each one level weaker
            foreach (Direction dir in Enum.GetValues<Direction>())
            {
                var next = current.Move(dir);
                if (_board.IsInBounds(next))
                {
                    seeds.Enqueue((next, power - 1));
                }
            }
        }
    }

    // Re-evaluates every torch based on the maximum power of its neighbors.
    // A torch turns OFF when any neighbor carries power (inversion logic).
    private void ResolveTorches()
    {
        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is not Torch torch) continue;

            int maxPower = MaxNeighborPower(pos);
            ((IPowerable)torch).UpdatePower(new SignalStrength(maxPower));
        }
    }

    // Handles the repeater delay queue. Three phases:
    //
    //   Phase 1 — advance counters.
    //     Each pending entry loses one tick. Those at 0 fire: if the output cell is a
    //     Wire or open space, it is seeded for BFS. If it is a Block, the block is
    //     recorded as "strongly powered" and its neighbors are seeded for BFS so that
    //     wires on the far side also receive signal.
    //
    //   Phase 2 — flood wires NOW.
    //     The BFS from fired repeaters runs before we read any repeater inputs. This is
    //     the key fix for chained repeaters: without this step the wire between A and B
    //     is still 0 when B checks its input, so B never gets scheduled.
    //
    //   Phase 3 — read inputs and schedule new outputs.
    //     For every repeater, we look at the cell on its input side. If that cell is a
    //     powered Block (from phase 1 or from a powered wire neighbor) we count it as
    //     power 15, covering the case: wire/source → block → repeater.
    private void ResolveRepeaters()
    {
        var next = new List<(Position, Direction, int)>();
        var seeds = new Queue<(Position, int)>();

        // --- Phase 1: advance pending queue ---
        while (_pendingRepeaters.Count > 0)
        {
            var (pos, dir, ticksLeft) = _pendingRepeaters.Dequeue();

            if (ticksLeft > 1)
            {
                next.Add((pos, dir, ticksLeft - 1));
            }
            else
            {
                var outPos = pos.Move(dir);
                if (_board.IsInBounds(outPos))
                {
                    var outEntity = _board.GetEntity(outPos);
                    if (outEntity is Block)
                    {
                        // The block is strongly powered.
                        // Flood its neighbors so wires on the far side receive signal.
                        _poweredBlocks.Add(outPos);
                        EnqueueNeighbors(outPos, 15, seeds);
                    }
                    else
                    {
                        seeds.Enqueue((outPos, 15));
                    }
                }
            }
        }

        // --- Phase 2: flood wires before reading downstream repeater inputs ---
        BfsFlood(seeds);

        // --- Phase 3: read inputs and schedule new outputs ---
        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is not Repeater rep) continue;

            var inputPos = pos.Move(Opposite(rep.Output));
            int inputPower = 0;

            if (_board.IsInBounds(inputPos))
            {
                var src = _board.GetEntity(inputPos);

                if (src is Block)
                {
                    // Block strongly powered by a fired repeater this tick
                    if (_poweredBlocks.Contains(inputPos))
                    {
                        inputPower = 15;
                    }
                    // Block powered by an adjacent wire, source, or torch
                    else if (IsBlockPowered(inputPos))
                    {
                        inputPower = 15;
                    }
                }
                else if (src is IPowerable pw)
                {
                    inputPower = pw.CurrentPower.Value;
                }
                else if (src is Lever lever && lever.IsOn)
                {
                    inputPower = 15;
                }
                else if (src is RedstoneBlock)
                {
                    inputPower = 15;
                }
                else if (src is Button btn && btn.IsPowering())
                {
                    inputPower = 15;
                }
            }

            ((IPowerable)rep).UpdatePower(new SignalStrength(inputPower));

            if (rep.IsPowering())
            {
                next.Add((pos, rep.Output, rep.Delay));
            }
        }

        foreach (var item in next)
        {
            _pendingRepeaters.Enqueue(item);
        }
    }

    // Lights lamps based on the maximum power among their four neighbors.
    // Lamps are resolved last so they see power from repeaters that fired this tick.
    private void ResolveLamps()
    {
        foreach (var pos in _board.AllPositions())
        {
            var e = _board.GetEntity(pos);
            if (e is not RedstoneLamp lamp) continue;
            lamp.UpdatePower(new SignalStrength(MaxNeighborPower(pos)));
        }
    }

    // Returns the highest power level found among the four neighbors of pos.
    // Used by both ResolveTorches and ResolveLamps.
    //
    // Rules:
    //   - Lamps are excluded: they receive power but do not re-emit it.
    //   - A Block in _poweredBlocks counts as power 15 (strongly powered by a repeater).
    //   - IPowerable entities (Wire, Torch, Repeater) contribute their CurrentPower.
    //   - Direct sources (Lever, RedstoneBlock, Button) contribute 15 when active.
    private int MaxNeighborPower(Position pos)
    {
        int max = 0;

        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            var np = pos.Move(dir);
            if (!_board.IsInBounds(np)) continue;

            var e = _board.GetEntity(np);

            if (e is RedstoneLamp) continue; // lamps don't emit signal

            int p = 0;

            if (e is Block && _poweredBlocks.Contains(np))
            {
                p = 15; // block powered by a fired repeater this tick
            }
            else if (e is IPowerable pw)
            {
                p = pw.CurrentPower.Value;
            }
            else if (e is Lever l && l.IsOn)
            {
                p = 15;
            }
            else if (e is RedstoneBlock)
            {
                p = 15;
            }
            else if (e is Button btn && btn.IsPowering())
            {
                p = 15;
            }

            if (p > max)
            {
                max = p;
            }
        }

        return max;
    }

    // Returns true if any neighbor of blockPos is actively providing power.
    // Used to detect the case: wire/source → block → repeater.
    // The check runs after BFS, so wire power levels are already final.
    private bool IsBlockPowered(Position blockPos)
    {
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            var np = blockPos.Move(dir);
            if (!_board.IsInBounds(np)) continue;

            var e = _board.GetEntity(np);

            if (e is Wire wire && wire.CurrentPower.Value > 0) return true;
            if (e is Lever l && l.IsOn) return true;
            if (e is RedstoneBlock) return true;
            if (e is Button btn && btn.IsPowering()) return true;
            if (e is Torch torch && torch.IsOn) return true;
            if (e is Repeater rep && rep.IsPowering()) return true;
        }

        return false;
    }

    // Returns the direction opposite to the given one (North↔South, East↔West).
    private static Direction Opposite(Direction d)
    {
        switch (d)
        {
            case Direction.North: return Direction.South;
            case Direction.South: return Direction.North;
            case Direction.East:  return Direction.West;
            case Direction.West:  return Direction.East;
            default:              return d;
        }
    }
}

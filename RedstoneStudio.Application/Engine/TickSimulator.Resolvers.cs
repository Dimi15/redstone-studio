using System.Collections.Generic;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

public partial class TickSimulator
{
    // Re-evaluates every torch based on the maximum power of its neighbors.
    // A torch turns off when any neighbor carries power (inversion logic).
    private void ResolveTorches()
    {
        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            if (!(entity is Torch))
            {
                continue;
            }

            Torch torch = (Torch)entity;

            int maxPower = MaxNeighborPower(pos);
            IPowerable powerableTorch = (IPowerable)torch;
            powerableTorch.UpdatePower(new SignalStrength(maxPower));
        }
    }

    // The repeater lifecycle processes in three steps: First, it decrements delays and fires active 
    // signals (powering wires or solid blocks). Next, it immediately runs the BFS to update connecting 
    // wires so chained repeaters don't stall. Finally, it reads inputs and queues newly powered repeaters.
    private void ResolveRepeaters()
    {
        List<(Position, Direction, int)> next = new List<(Position, Direction, int)>();
        Queue<(Position, int)> seeds = new Queue<(Position, int)>();

        // Advance pending queue
        while (_pendingRepeaters.Count > 0)
        {
            (Position pos, Direction dir, int ticksLeft) = _pendingRepeaters.Dequeue();

            if (ticksLeft > 1)
            {
                next.Add((pos, dir, ticksLeft - 1));
            }
            else
            {
                Position outPos = pos.Move(dir);
                if (_board.IsInBounds(outPos))
                {
                    Entity outEntity = _board.GetEntity(outPos);
                    if (outEntity is Block)
                    {
                        // The block is strongly powered
                        // Flood its neighbors so wires on the far side receive signal
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

        // Flood wires before reading downstream repeater inputs
        BfsFlood(seeds);

        // Read inputs and schedule new outputs
        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            if (!(entity is Repeater))
            {
                continue;
            }

            Repeater rep = (Repeater)entity;

            Position inputPos = pos.Move(Opposite(rep.Output));
            int inputPower = 0;

            if (_board.IsInBounds(inputPos))
            {
                Entity src = _board.GetEntity(inputPos);

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
                else if (src is IPowerable)
                {
                    IPowerable pw = (IPowerable)src;
                    inputPower = pw.CurrentPower.Value;
                }
                else if (src is Lever)
                {
                    Lever lever = (Lever)src;
                    if (lever.IsOn)
                    {
                        inputPower = 15;
                    }
                }
                else if (src is RedstoneBlock)
                {
                    inputPower = 15;
                }
                else if (src is Button)
                {
                    Button btn = (Button)src;
                    if (btn.IsPowering())
                    {
                        inputPower = 15;
                    }
                }
            }

            IPowerable powerableRep = (IPowerable)rep;
            powerableRep.UpdatePower(new SignalStrength(inputPower));

            if (rep.IsPowering())
            {
                next.Add((pos, rep.Output, rep.Delay));
            }
        }

        for (int i = 0; i < next.Count; i++)
        {
            (Position repeaterPos, Direction repeaterDir, int repeaterTicksLeft) = next[i];
            _pendingRepeaters.Enqueue((repeaterPos, repeaterDir, repeaterTicksLeft));
        }
    }

    // Lights lamps based on the maximum power among their four neighbors.
    // Lamps are resolved last so they see power from repeaters that fired this tick.
    private void ResolveLamps()
    {
        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            if (!(entity is RedstoneLamp))
            {
                continue;
            }

            RedstoneLamp lamp = (RedstoneLamp)entity;
            lamp.UpdatePower(new SignalStrength(MaxNeighborPower(pos)));
        }
    }

    // Returns the max power from 4 neighbors for Torches/Lamps.
    // Contributors: Active direct sources and strongly powered blocks provide 15.
    // IPowerable entities (Wires/Torches/Repeaters) provide their CurrentPower.
    // Lamps are ignored as they act only as power sinks.
    private int MaxNeighborPower(Position pos)
    {
        int max = 0;

        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            Position np = pos.Move(dir);
            if (!_board.IsInBounds(np))
            {
                continue;
            }

            Entity entity = _board.GetEntity(np);

            if (entity is RedstoneLamp)
            {
                continue; // lamps don't emit signal
            }

            int p = 0;

            if (entity is Block)
            {
                if (_poweredBlocks.Contains(np))
                {
                    p = 15; // block powered by a fired repeater this tick
                }
            }
            else if (entity is IPowerable)
            {
                IPowerable pw = (IPowerable)entity;
                p = pw.CurrentPower.Value;
            }
            else if (entity is Lever)
            {
                Lever lever = (Lever)entity;
                if (lever.IsOn)
                {
                    p = 15;
                }
            }
            else if (entity is RedstoneBlock)
            {
                p = 15;
            }
            else if (entity is Button)
            {
                Button btn = (Button)entity;
                if (btn.IsPowering())
                {
                    p = 15;
                }
            }

            if (p > max)
            {
                max = p;
            }
        }

        return max;
    }

    // Returns true if any neighbor of blockPos is actively providing power.
    // Used to detect the case: wire/source > block > repeater.
    // The check runs after BFS, so wire power levels are already final.
    private bool IsBlockPowered(Position blockPos)
    {
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            Position np = blockPos.Move(dir);
            if (!_board.IsInBounds(np))
            {
                continue;
            }

            Entity entity = _board.GetEntity(np);

            if (entity is Wire)
            {
                Wire wire = (Wire)entity;
                if (wire.CurrentPower.Value > 0)
                {
                    return true;
                }
            }
            else if (entity is Lever)
            {
                Lever lever = (Lever)entity;
                if (lever.IsOn)
                {
                    return true;
                }
            }
            else if (entity is RedstoneBlock)
            {
                return true;
            }
            else if (entity is Button)
            {
                Button btn = (Button)entity;
                if (btn.IsPowering())
                {
                    return true;
                }
            }
            else if (entity is Torch)
            {
                Torch torch = (Torch)entity;
                if (torch.IsOn)
                {
                    return true;
                }
            }
            else if (entity is Repeater)
            {
                Repeater rep = (Repeater)entity;
                if (rep.IsPowering())
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Returns the direction opposite to the given one (North <> South, East <> West).
    private static Direction Opposite(Direction d)
    {
        switch (d)
        {
            case Direction.North:
                return Direction.South;
            case Direction.South:
                return Direction.North;
            case Direction.East:
                return Direction.West;
            case Direction.West:
                return Direction.East;
            default:
                return d;
        }
    }
}
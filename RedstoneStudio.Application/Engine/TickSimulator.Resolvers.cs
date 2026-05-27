using System.Collections.Generic;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

public partial class TickSimulator
{
    // Re-evaluates every torch based on the maximum power of its neighbors.
    // A torch turns off when any neighbor carries power.
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

    // The repeater lifecycle is composed of three steps:
    // It decrements delays and fires active signals (powering wires or solid blocks).
    // Next it immediately runs the BFS to update connecting wires.
    // Finally it reads inputs and queues newly powered repeaters.
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

        // Flood wires
        BfsFlood(seeds);

        // Read repeaters inputs and schedule new outputs
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
                    // Hard pow
                    if (_poweredBlocks.Contains(inputPos))
                    {
                        inputPower = 15;
                    }
                    // Soft pow
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

    // Lamps are resolved last so they can see power from repeaters that fired this tick.
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

    // Returns the max power from 4 neighbors for torches or lamps.
    // Active direct sources and strongly powered blocks provide 15 power.
    // IPowerable entities (Wires/Torches/Repeaters) provide their CurrentPower.
    // Lamps are ignored as they only "consume" power.
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
    // Made for case source > block > repeater.
    // The check runs after BFS so that wire power levels are already in the final stage.
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

    // Helper function that returns the direction opposite to the given one.
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
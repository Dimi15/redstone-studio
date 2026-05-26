using System.Collections.Generic;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

// Signal collection and BFS flood-fill propagation.
public partial class TickSimulator
{
    // Calls Tick() on every ITickable entity (currently only Button).
    private void AdvanceTickables()
    {
        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            if (entity is ITickable)
            {
                ITickable tickable = (ITickable)entity;
                tickable.Tick();
            }
        }
    }

    // Sets every IPowerable entity back to power 0 so the tick starts from a clean state.
    // Also clears the powered-blocks set from the previous tick.
    private void ResetAllPower()
    {
        _poweredBlocks.Clear();

        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            if (entity is IPowerable)
            {
                IPowerable powerable = (IPowerable)entity;
                powerable.UpdatePower(SignalStrength.Min);
            }
        }
    }

    // Finds every active source (lever on, button pulsing, redstone block) and
    // enqueues its four neighbors at full power 15.
    // We enqueue neighbors, not the source itself, because the BFS only
    // propagates through Wire cells, and sources are not wires.
    private Queue<(Position, int)> CollectSources()
    {
        Queue<(Position, int)> queue = new Queue<(Position, int)>();

        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            bool isActiveSource = false;

            if (entity is Lever)
            {
                Lever lever = (Lever)entity;
                if (lever.IsOn)
                {
                    isActiveSource = true;
                }
            }
            else if (entity is RedstoneBlock)
            {
                isActiveSource = true;
            }
            else if (entity is Button)
            {
                Button button = (Button)entity;
                if (button.IsPowering())
                {
                    isActiveSource = true;
                }
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
        Queue<(Position, int)> queue = new Queue<(Position, int)>();

        foreach (Position pos in _board.AllPositions())
        {
            Entity entity = _board.GetEntity(pos);
            if (entity is Torch)
            {
                Torch torch = (Torch)entity;
                if (torch.IsOn)
                {
                    EnqueueNeighbors(pos, 15, queue);
                }
            }
        }

        return queue;
    }

    // Adds all four in-bounds neighbors of pos to the queue at the given power level.
    private void EnqueueNeighbors(Position pos, int power, Queue<(Position, int)> queue)
    {
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            Position neighbor = pos.Move(dir);
            if (_board.IsInBounds(neighbor))
            {
                queue.Enqueue((neighbor, power));
            }
        }
    }

    // Propagates power through Wires using BFS, starting from neighbors of power sources (at 15).
    // Power drops by 1 per tile. Wires only update if the incoming signal is stronger than
    // their current power, which naturally prevents infinite loops without a visited set.
    private void BfsFlood(Queue<(Position pos, int power)> seeds)
    {
        while (seeds.Count > 0)
        {
            (Position current, int power) = seeds.Dequeue();

            if (!_board.IsInBounds(current))
            {
                continue;
            }

            if (power <= 0)
            {
                continue;
            }

            Entity entity = _board.GetEntity(current);

            if (!(entity is Wire))
            {
                continue; //only Wire cells carry signal
            }

            Wire wire = (Wire)entity;

            if (power <= wire.CurrentPower.Value)
            {
                continue; //already at a higher or equal level
            }

            wire.UpdatePower(new SignalStrength(power));

            // Spread to all four neighbors, each one level weaker
            foreach (Direction dir in Enum.GetValues<Direction>())
            {
                Position next = current.Move(dir);
                if (_board.IsInBounds(next))
                {
                    seeds.Enqueue((next, power - 1));
                }
            }
        }
    }
}

using System.Collections.Generic;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Engine;

// Drives one tick of the redstone simulation.
// The class is split across three files for readability:
//   TickSimulator.cs          — fields, Tick(), Reset()
//   TickSimulator.Propagation.cs — signal collection and BFS flood
//   TickSimulator.Resolvers.cs   — torches, repeaters, lamps
public partial class TickSimulator
{
    private readonly CircuitBoard _board;

    // Each entry is a repeater counting down before it fires:
    // (position, output direction, ticks remaining).
    private readonly Queue<(Position pos, Direction outputDir, int ticksLeft)> _pendingRepeaters
        = new Queue<(Position pos, Direction outputDir, int ticksLeft)>();

    // Blocks powered this tick by a fired repeater.
    // Populated in ResolveRepeaters(), cleared at the start of each tick.
    // Used so that lamps, torches, and other repeaters can detect a powered block.
    private readonly HashSet<Position> _poweredBlocks = new HashSet<Position>();

    public int TickCount { get; private set; }

    public TickSimulator(CircuitBoard board)
    {
        _board = board;
    }

    // Runs one full simulation step. The order of the phases matters:
    // sources must be flooded before torches read wire power, and
    // repeaters must fire before lamps check their neighbors.
    public void Tick()
    {
        TickCount++;
        AdvanceTickables(); // 1. button countdown
        ResetAllPower(); // 2. wipe all power to 0 (also clears _poweredBlocks)
        BfsFlood(CollectSources()); // 3. flood from lever / button / redstone block
        ResolveTorches(); // 4. flip torches based on updated wire power
        BfsFlood(CollectTorchSources()); // 5. flood from lit torches
        ResolveRepeaters(); // 6. tick down delay queue, flood from fired repeaters
        ResolveLamps(); // 7. light lamps based on neighbor power
    }

    // Resets the tick counter and empties the repeater delay queue.
    public void Reset()
    {
        TickCount = 0;
        _pendingRepeaters.Clear();
    }
}
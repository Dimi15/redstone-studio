# CHALLENGE_PLAN.md — Redstone Studio
> Execution roadmap, progress tracker, and handoff reference.

---

## Architecture Overview

```
redstoneStudio.UI              Avalonia 12 (MVVM, ViewModels, AXAML)
RedstoneStudio.Application     Simulation engine, grid management
RedstoneStudio.Domain          Entities, enums, value objects, interfaces
RedstoneStudio.Infrastructure  JSON persistence (Save / Load)
```

All namespaces use the `redstone_stone.*` convention throughout the solution.  
The UI project does **not** have `<ImplicitUsings>` — all ViewModels and Views must include explicit `using System;` / `using System.Threading.Tasks;` etc.

---

## Component State Logic

### Entity Hierarchy

| Entity | Role | Key interface(s) |
|---|---|---|
| `Air` | Empty cell — no behaviour | — |
| `Block` | Solid, non-conducting | — |
| `Wire` | Carries signal (0-15); BFS engine sets power | `IPowerable`, `IConnectable` |
| `Lever` | Toggle on/off power source | — |
| `Button` | Pulse source — fires for N ticks then auto-off | `ITickable` |
| `RedstoneBlock` | Always-on power source (constant 15) | — |
| `Torch` | Inverter — on when input = 0, off otherwise | `IPowerable`, `IConnectable` |
| `Repeater` | Amplifier + delay (1-4 ticks), directional | `IPowerable`, `IConnectable` |
| `RedstoneLamp` | Output indicator — lights when powered, terminal node | `IPowerable` |

### Interfaces

| Interface | Method(s) | Purpose |
|---|---|---|
| `IPowerable` | `CurrentPower { get; }`, `UpdatePower(SignalStrength)` | Blocks that receive/store signal |
| `IConnectable` | `CanConnectTo(Direction)` | Directional connectivity rules |
| `ITickable` | `Tick()` | Time-based countdown (Button) |

### Signal Flow — one tick

```
1. AdvanceTickables()        → Button counts down its pulse
2. ResetAllPower()           → All IPowerable blocks set to 0
3. CollectSources()          → Enqueue 4 NEIGHBORS of Lever/Button/RedstoneBlock at power 15
4. BfsFlood(seeds)           → Power floods through Wires only (−1/hop); no visited set
5. ResolveTorches()          → Each torch reads MaxNeighborPower, inverts state
6. BfsFlood(CollectTorchSources()) → UNCONDITIONAL flood from lit torches (not conditional on change)
7. ResolveRepeaters()        → Tick-down pending outputs; enqueue new ones with delay
8. ResolveLamps()            → Each lamp = MaxNeighborPower (no BFS decay on last hop)
```

Key invariant: BFS seeds neighbors of sources (not sources themselves), because sources are not Wire/Lamp and would silently drop out of the BFS otherwise.

### Direction Enum

`North = 0 (Y−1)` · `East = 1 (X+1)` · `South = 2 (Y+1)` · `West = 3 (X−1)`  
`Opposite(d) = (d + 2) % 4`

---

## JSON Schema

Only **non-Air** cells are persisted.

```json
{
  "Width": 20,
  "Height": 15,
  "Cells": [
    { "X": 3,  "Y": 5, "Type": "Lever",        "IsOn": true },
    { "X": 4,  "Y": 5, "Type": "Wire" },
    { "X": 6,  "Y": 5, "Type": "Torch",     "Direction": "West", "Vertical": false },
    { "X": 8,  "Y": 5, "Type": "Repeater",  "Direction": "East", "Delay": 2 },
    { "X": 10, "Y": 5, "Type": "Button" },
    { "X": 12, "Y": 5, "Type": "RedstoneLamp" },
    { "X": 14, "Y": 5, "Type": "RedstoneBlock" },
    { "X": 16, "Y": 5, "Type": "Block" }
  ]
}
```

---

## File Map

| File | Layer | Status |
|---|---|---|
| `Domain/Enums/Direction.cs` | Domain | ✅ enum North/East/South/West |
| `Domain/Enums/Blocktype.cs` | Domain | ✅ enum BlockType (9 values) |
| `Domain/Entities/Entity.cs` | Domain | ✅ abstract base |
| `Domain/Entities/Air.cs` | Domain | ✅ |
| `Domain/Entities/Block.cs` | Domain | ✅ |
| `Domain/Entities/Wire.cs` | Domain | ✅ IPowerable + IConnectable |
| `Domain/Entities/Lever.cs` | Domain | ✅ toggle source |
| `Domain/Entities/Button.cs` | Domain | ✅ ITickable pulse source |
| `Domain/Entities/RedstoneBlock.cs` | Domain | ✅ constant source |
| `Domain/Entities/Torch.cs` | Domain | ✅ inverter |
| `Domain/Entities/Repeater.cs` | Domain | ✅ delay amplifier |
| `Domain/Entities/RedstoneLamp.cs` | Domain | ✅ terminal output |
| `Domain/Interfaces/IPowerable.cs` | Domain | ✅ |
| `Domain/Interfaces/IConnectable.cs` | Domain | ✅ |
| `Domain/Interfaces/ITickable.cs` | Domain | ✅ |
| `Domain/ValueObjects/Position.cs` | Domain | ✅ Move(Direction) |
| `Domain/ValueObjects/SignalStrength.cs` | Domain | ✅ 0-15 clamped |
| `Application/Grid/CircuitBoard.cs` | Application | ✅ Width/Height/IsInBounds/AllPositions |
| `Application/Engine/TickSimulator.cs` | Application | ✅ BFS + torch + repeater |
| `Infrastructure/Persistance/CircuitData.cs` | Infrastructure | ✅ DTOs |
| `Infrastructure/Persistance/JsonCircuitRepository.cs` | Infrastructure | ✅ async Save/Load |
| `UI/ViewModels/ViewModelBase.cs` | UI | ✅ ObservableObject |
| `UI/ViewModels/CellViewModel.cs` | UI | ✅ Symbol/CellColor/ForeColor |
| `UI/ViewModels/MainWindowViewModel.cs` | UI | ✅ commands + grid |
| `UI/Views/MainWindow.axaml` | UI | ✅ grid + toolbar + status bar |
| `UI/Views/MainWindow.axaml.cs` | UI | ✅ file picker handlers |

---

## Known Bugs Fixed

| Bug | Root Cause | Fix |
|---|---|---|
| Wire double-decay | BFS decremented before enqueue; Wire.UpdatePower decremented again | Wire.UpdatePower now stores value directly |
| BFS never propagated past sources | CollectSources seeded source positions; BFS only handles Wire/Lamp so sources silently dropped | CollectSources now seeds 4 NEIGHBORS of each source at power 15 |
| Lamp never lit on last hop | Wire at power 1 enqueued lamp at power 0 → lamp.CurrentPower stays 0 | Removed lamps from BFS; added ResolveLamps() pass using MaxNeighborPower |
| Visited HashSet blocked stronger signal | Plain Queue + HashSet: first (weaker) path marks visited, stronger path blocked | Removed HashSet; use `power <= wire.CurrentPower.Value` guard only |
| Torch reads stale state | Torch resolved before wires settled | Two-pass: wire BFS → resolve torches → unconditional second wire BFS |
| Torch BFS flood skipped | ResetAllPower calls UpdatePower(0) on Torch → _on=true after reset; wasPowering always true → anyChanged always false → second BFS never ran | Made torch BFS unconditional; removed conditional guard |
| Compiled binding `$parent[Window]` | Avalonia compiled bindings don't support ancestor lookup | Each CellViewModel holds `Action<CellViewModel>` callback; `x:CompileBindings="False"` on DataTemplate |

---

## Current Progress

- [x] All entities implemented and compiling
- [x] BFS tick engine working — sources flood neighbors, Torch inversion, Repeater delay, ResolveLamps pass
- [x] JSON Save / Load functional
- [x] Full Avalonia UI with toolbar, grid, status bar
- [x] `dotnet build` → **0 warnings, 0 errors**
- [x] UML split into PROJECT_USECASE.puml and PROJECT_CLASSES.puml (PROJECT_DIAGRAMS.puml is superseded)

## Possible Future Improvements

- Pressure plate (auto-off on next tick)
- Comparator (subtract mode / compare mode)
- Larger/resizable grid
- Undo/redo stack
- Wire drag-to-paint (hold mouse button)
- Keyboard shortcut for each tool

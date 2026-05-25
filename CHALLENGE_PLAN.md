# Redstone Studio — Challenge Plan

## Architecture Overview

Clean Architecture with 4 layers:

```
redstoneStudio.UI          → Avalonia UI (MVVM, ViewModels, AXAML views)
RedstoneStudio.Application → Simulation engine, grid management
RedstoneStudio.Domain      → Entities, value objects, interfaces
RedstoneStudio.Infrastructure → JSON persistence
```

All namespaces use the `redstone_stone.*` convention (not the csproj RootNamespace).

---

## Component State Logic

### Entity Hierarchy
```
Entity (abstract)
├── Air            — empty cell, no power
├── Block          — solid non-conducting block
├── Lever          — toggles _on; IsPowering() = _on
├── Button         — ITickable; emits power for N ticks after ChangeState(); auto-off
├── RedstoneBlock  — always IsPowering() = true
├── Wire           — IPowerable; CurrentPower set by BFS engine (range 0–15)
├── Torch          — IPowerable, IConnectable; inverts: on if input=0, off if input>0
├── Repeater       — IPowerable, IConnectable; outputs 15 if input>0, with tick delay 1–4
└── RedstoneLamp   — IPowerable; lights up when powered, terminal node (no output)
```

### Interfaces
- `IPowerable` — `CurrentPower` property + `UpdatePower(SignalStrength)` method
- `IConnectable` — `CanConnectTo(Direction)` method
- `ITickable` — `Tick()` method; called once per simulator tick (for time-based entities)

### Signal Flow (one tick)
1. Reset all Wire/Torch/Repeater CurrentPower to 0
2. BFS flood from all active sources (Lever=on, RedstoneBlock, Torch=on)
3. Wire power decays by 1 per step; each wire takes the max it receives
4. After wire flood: re-evaluate Torches (on if no neighbor has power)
5. If any Torch changed state → run another wire flood from newly-lit torches
6. Repeater pass: if input-side neighbor is powered, queue output after _delay ticks

### Direction Enum
`North = up (Y-1), East = right (X+1), South = down (Y+1), West = left (X-1)`

---

## JSON Schema

File extension: `.json`

```json
{
  "Width": 20,
  "Height": 15,
  "Cells": [
    { "X": 3,  "Y": 5, "Type": "Lever",        "IsOn": true },
    { "X": 4,  "Y": 5, "Type": "Wire" },
    { "X": 5,  "Y": 5, "Type": "Wire" },
    { "X": 6,  "Y": 5, "Type": "Torch",     "Direction": "West",  "Vertical": false },
    { "X": 8,  "Y": 5, "Type": "Repeater",  "Direction": "East",  "Delay": 2 },
    { "X": 10, "Y": 5, "Type": "RedstoneBlock" },
    { "X": 11, "Y": 5, "Type": "Block" }
  ]
}
```

Only non-Air cells are serialized. `Direction` and `Delay` fields are omitted for types that don't use them.

---

## Key Files

| File | Purpose |
|------|---------|
| `Domain/Enums/Direction.cs` | North/East/South/West enum |
| `Domain/Enums/Blocktype.cs` | Air/Block/Wire/Lever/RedstoneBlock/Torch/Repeater enum |
| `Domain/Entities/*.cs` | Entity implementations |
| `Domain/ValueObjects/Position.cs` | X/Y coordinate + Move(Direction) helper |
| `Domain/ValueObjects/SignalStrength.cs` | Power level 0–15 (clamped) |
| `Application/Grid/CircuitBoard.cs` | 2D Entity grid |
| `Application/Engine/TickSimulator.cs` | BFS power propagation + repeater delays |
| `Infrastructure/Persistance/JsonCircuitRepository.cs` | Async JSON Save/Load |
| `UI/ViewModels/CellViewModel.cs` | Per-cell observable state (symbol, color) |
| `UI/ViewModels/MainWindowViewModel.cs` | Main VM: grid, tools, simulation commands |
| `UI/Views/MainWindow.axaml` | Grid UI, toolbar, simulation controls |

---

## Current Progress & Handoff Checklist

### ✅ Completed
- [x] CHALLENGE_PLAN.md created (this file)

### ✅ Domain Layer
- [x] `Direction.cs` → changed class to enum (North, East, South, West)
- [x] `Blocktype.cs` → changed class to enum BlockType (Air, Block, Wire, Lever, RedstoneBlock, Torch, Repeater)
- [x] `Entity.cs` → made `position` public (`public Position Position { get; set; }`)
- [x] `Position.cs` → added `Position(int x, int y)` constructor, `Move(Direction)` method, `Equals`/`GetHashCode`
- [x] `Wire.cs` → fixed `UpdatePower` to assign `CurrentPower = new SignalStrength(decayedPower)`
- [x] `Repeater.cs` → fixed `CurrentPower` property, fixed `UpdatePower`, implemented `CanConnectTo`, added `Output`/`Delay` accessors
- [x] `Torch.cs` → fully implemented (was commented out with invalid syntax)
- [x] `Lever.cs` → added `public bool IsOn => _on`

### ✅ Application Layer
- [x] `CircuitBoard.cs` → added `Width`, `Height`, `IsInBounds(Position)`, `AllPositions()`
- [x] `TickSimulator.cs` → implemented BFS flood fill with Torch inversion and Repeater delay queue

### ✅ Infrastructure Layer
- [x] `JsonCircuitRepository.cs` → added `CircuitData`/`CellData` DTOs; implemented `SaveAsync`/`LoadAsync`

### ✅ UI Layer
- [x] Created `CellViewModel.cs` — observable per-cell state with Symbol and CellColor computed properties
- [x] Rewrote `MainWindowViewModel.cs` — grid ObservableCollection, tool selection, simulation commands
- [x] Rewrote `MainWindow.axaml` — DockPanel with toolbar, grid (ItemsControl + UniformGrid), status bar
- [x] Updated `MainWindow.axaml.cs` — file picker handlers for Save/Load

### ✅ Build
- [x] `dotnet build` passes clean (0 warnings, 0 errors)
- [x] App launches without crashes

### 🔲 Manual Testing (do these to verify)
- [ ] Wire placement + Lever toggle → power propagates visually (red wire gradient)
- [ ] Play/Pause/Step/Reset buttons work
- [ ] Torch turns off when input wire is powered
- [ ] Repeater passes signal with configurable delay (click repeater to cycle 1→4)
- [ ] Save → JSON file created with non-Air cells only
- [ ] Load → circuit restored from JSON

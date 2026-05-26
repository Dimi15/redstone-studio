# Redstone Studio — Diagramma delle Classi

```mermaid
classDiagram
    namespace DomainInterfaces {
        class IPowerable {
            <<interface>>
            +CurrentPower SignalStrength
            +UpdatePower(s SignalStrength) void
        }
        class IConnectable {
            <<interface>>
            +CanConnectTo(d Direction) bool
        }
        class ITickable {
            <<interface>>
            +Tick() void
        }
    }

    namespace DomainValueObjects {
        class Position {
            +X int
            +Y int
            +Move(d Direction) Position
            +Equals(obj object) bool
            +GetHashCode() int
        }
        class SignalStrength {
            +Value int
            +Min$ SignalStrength
            +Max$ SignalStrength
        }
    }

    namespace DomainEntities {
        class Entity {
            <<abstract>>
            +IsPowered() bool
            +IsPowering() bool
            +ChangeState() void
        }
        class Air
        class Block
        class Lever {
            +IsOn bool
            +IsPowering() bool
            +ChangeState() void
        }
        class Button {
            +IsPowering() bool
            +ChangeState() void
            +Tick() void
        }
        class RedstoneBlock {
            +IsPowering() bool
        }
        class Wire {
            +CurrentPower SignalStrength
            +UpdatePower(s SignalStrength) void
            +CanConnectTo(d Direction) bool
        }
        class Torch {
            +CurrentPower SignalStrength
            +IsOn bool
            +Placement Direction
            +IsPowering() bool
            +UpdatePower(s SignalStrength) void
            +CanConnectTo(d Direction) bool
        }
        class Repeater {
            +CurrentPower SignalStrength
            +Output Direction
            +Delay int
            +IsPowering() bool
            +ChangeState() void
            +UpdatePower(s SignalStrength) void
            +CanConnectTo(d Direction) bool
        }
        class RedstoneLamp {
            +CurrentPower SignalStrength
            +IsPowered() bool
            +UpdatePower(s SignalStrength) void
        }
    }

    namespace Application {
        class CircuitBoard {
            +Width int
            +Height int
            +GetEntity(p Position) Entity
            +PlaceEntity(e Entity, p Position) void
            +RemoveEntity(p Position) void
            +IsInBounds(p Position) bool
            +AllPositions() List~Position~
        }
        class TickSimulator {
            +TickCount int
            +Tick() void
            +Reset() void
        }
    }

    namespace Infrastructure {
        class JsonCircuitRepository {
            <<static>>
            +SaveAsync(b CircuitBoard, path string)$ Task
            +LoadAsync(path string)$ Task~CircuitBoard~
            +Snapshot(b CircuitBoard)$ CircuitData
            +FromSnapshot(d CircuitData)$ CircuitBoard
        }
        class CircuitData {
            +Width int
            +Height int
            +Cells List~CellData~
        }
        class CellData {
            +X int
            +Y int
            +Type string
            +Direction string
            +IsOn bool
            +Delay int
            +Vertical bool
        }
    }

    namespace UIViewModels {
        class ViewModelBase {
            <<abstract>>
        }
        class CellViewModel {
            +X int
            +Y int
            +BlockType BlockType
            +IsOn bool
            +PowerLevel int
            +Direction Direction
            +Symbol string
            +CellColor string
            +ForeColor string
            +ClickCommand IRelayCommand
            +Refresh() void
        }
        class MainWindowViewModel {
            +GridWidth int
            +GridHeight int
            +Cells ObservableCollection~CellViewModel~
            +SelectedBlockType BlockType
            +SelectedDirection Direction
            +IsRunning bool
            +TickCount int
            +SaveCircuit(path string) Task
            +LoadCircuit(path string) Task
            +HistoryBack() void
            +HistoryForward() void
        }
    }

    %% Ereditarietà
    Entity <|-- Air
    Entity <|-- Block
    Entity <|-- Wire
    Entity <|-- Lever
    Entity <|-- Button
    Entity <|-- RedstoneBlock
    Entity <|-- Torch
    Entity <|-- Repeater
    Entity <|-- RedstoneLamp

    %% Implementazione interfacce
    Wire ..|> IPowerable
    Wire ..|> IConnectable
    Torch ..|> IPowerable
    Torch ..|> IConnectable
    Repeater ..|> IPowerable
    Repeater ..|> IConnectable
    RedstoneLamp ..|> IPowerable
    Button ..|> ITickable

    %% Gerarchia UI
    ViewModelBase <|-- CellViewModel
    ViewModelBase <|-- MainWindowViewModel

    %% Dipendenze architetturali
    CircuitBoard "1" *-- "many" Entity
    TickSimulator --> CircuitBoard
    JsonCircuitRepository --> CircuitBoard
    JsonCircuitRepository --> CircuitData
    MainWindowViewModel --> CircuitBoard
    MainWindowViewModel --> TickSimulator
    MainWindowViewModel --> JsonCircuitRepository
    MainWindowViewModel "1" *-- "many" CellViewModel
```

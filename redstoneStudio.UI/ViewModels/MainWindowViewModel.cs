using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using redstone_stone.Application.Engine;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;
using redstone_stone.Infrastructure.Persistance;

namespace redstone_stone.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public int GridWidth  { get; } = 40;
    public int GridHeight { get; } = 20;

    public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();

    [ObservableProperty] private BlockType _selectedBlockType = BlockType.Wire;
    [ObservableProperty] private Direction _selectedDirection = Direction.North;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int _tickCount;
    [ObservableProperty] private string _statusText = "Pick a tool above, then click the grid";

    private CircuitBoard _board;
    private TickSimulator _simulator;
    private readonly DispatcherTimer _timer;

    // Undo/redo stacks store board snapshots as plain data (no domain objects).
    private readonly List<CircuitData> _undoStack = new List<CircuitData>();
    private readonly List<CircuitData> _redoStack = new List<CircuitData>();

    public MainWindowViewModel()
    {
        _board     = new CircuitBoard(GridWidth, GridHeight);
        _simulator = new TickSimulator(_board);

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(200);
        _timer.Tick += OnTimerTick;

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                Cells.Add(new CellViewModel(x, y, OnCellClicked));
            }
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        DoStep();
    }

    // ── Simulation commands ──────────────────────────────────────────────────

    [RelayCommand]
    private void Play()
    {
        _timer.Start();
        IsRunning  = true;
        StatusText = "Running…";
    }

    [RelayCommand]
    private void Pause()
    {
        _timer.Stop();
        IsRunning  = false;
        StatusText = $"Paused at tick {TickCount}";
    }

    [RelayCommand]
    private void Step()
    {
        DoStep();
    }

    [RelayCommand]
    private void Reset()
    {
        _timer.Stop();
        IsRunning  = false;
        _board     = new CircuitBoard(GridWidth, GridHeight);
        _simulator = new TickSimulator(_board);
        TickCount  = 0;

        _undoStack.Clear();
        _redoStack.Clear();
        HistoryBackCommand.NotifyCanExecuteChanged();
        HistoryForwardCommand.NotifyCanExecuteChanged();

        foreach (var cell in Cells)
        {
            cell.BlockType = BlockType.Air;
            cell.Refresh();
        }

        StatusText = "Board cleared";
    }

    // ── Undo / Redo ──────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void HistoryBack()
    {
        _redoStack.Add(JsonCircuitRepository.Snapshot(_board));
        var snapshot = _undoStack[_undoStack.Count - 1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ApplySnapshot(snapshot);
        HistoryBackCommand.NotifyCanExecuteChanged();
        HistoryForwardCommand.NotifyCanExecuteChanged();
        StatusText = $"Undo  ({_undoStack.Count} step{(_undoStack.Count == 1 ? "" : "s")} left)";
    }

    private bool CanUndo()
    {
        return _undoStack.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void HistoryForward()
    {
        _undoStack.Add(JsonCircuitRepository.Snapshot(_board));
        var snapshot = _redoStack[_redoStack.Count - 1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ApplySnapshot(snapshot);
        HistoryBackCommand.NotifyCanExecuteChanged();
        HistoryForwardCommand.NotifyCanExecuteChanged();
        StatusText = $"Redo  ({_redoStack.Count} step{(_redoStack.Count == 1 ? "" : "s")} left)";
    }

    private bool CanRedo()
    {
        return _redoStack.Count > 0;
    }

    // ── Tool selection ───────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectTool(string tool)
    {
        if (Enum.TryParse<BlockType>(tool, out var blockType))
        {
            SelectedBlockType = blockType;
            StatusText = $"Tool: {blockType}";
        }
    }

    [RelayCommand]
    private void RotateDirection()
    {
        int next = ((int)SelectedDirection + 1) % 4;
        SelectedDirection = (Direction)next;
        StatusText = $"Direction: {SelectedDirection}";
    }

    // ── File I/O ─────────────────────────────────────────────────────────────
    // Called directly from code-behind after the file picker returns a path.

    public async Task SaveCircuit(string path)
    {
        await JsonCircuitRepository.SaveAsync(_board, path);
        StatusText = $"Saved → {Path.GetFileName(path)}";
    }

    public async Task LoadCircuit(string path)
    {
        _timer.Stop();
        IsRunning  = false;
        _board     = await JsonCircuitRepository.LoadAsync(path);
        _simulator = new TickSimulator(_board);
        TickCount  = 0;

        _undoStack.Clear();
        _redoStack.Clear();
        HistoryBackCommand.NotifyCanExecuteChanged();
        HistoryForwardCommand.NotifyCanExecuteChanged();

        // If the loaded circuit has different dimensions, rebuild the cell list.
        if (_board.Width != GridWidth || _board.Height != GridHeight)
        {
            Cells.Clear();
            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    Cells.Add(new CellViewModel(x, y, OnCellClicked));
                }
            }
        }

        RefreshAll();
        StatusText = $"Loaded ← {Path.GetFileName(path)}";
    }

    // ── Grid interaction ──────────────────────────────────────────────────────

    private void OnCellClicked(CellViewModel cell)
    {
        // Snapshot the current state before applying any change (enables undo).
        _undoStack.Add(JsonCircuitRepository.Snapshot(_board));
        _redoStack.Clear();
        if (_undoStack.Count > 50)
        {
            _undoStack.RemoveAt(0);
        }
        HistoryBackCommand.NotifyCanExecuteChanged();
        HistoryForwardCommand.NotifyCanExecuteChanged();

        var pos      = new Position(cell.X, cell.Y);
        var existing = _board.GetEntity(pos);

        if (SelectedBlockType == BlockType.Air)
        {
            // Erase tool: remove whatever is there.
            _board.RemoveEntity(pos);
        }
        else if (EntityToBlockType(existing) == SelectedBlockType)
        {
            // Same type already placed: toggle or cycle its state.
            existing.ChangeState();
        }
        else
        {
            // Different type: replace with the selected block.
            var newEntity = CreateEntity(SelectedBlockType, SelectedDirection);
            _board.PlaceEntity(newEntity, pos);
        }

        SyncCell(cell);
        DoStep();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void DoStep()
    {
        _simulator.Tick();
        TickCount = _simulator.TickCount;
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (var cell in Cells)
        {
            SyncCell(cell);
        }
    }

    // Restores the board from a CircuitData snapshot (used for undo/redo).
    private void ApplySnapshot(CircuitData snapshot)
    {
        _board = JsonCircuitRepository.FromSnapshot(snapshot);
        _simulator = new TickSimulator(_board);
        RefreshAll();
    }

    // Copies domain entity state into its matching CellViewModel.
    private void SyncCell(CellViewModel vm)
    {
        var entity = _board.GetEntity(new Position(vm.X, vm.Y));

        vm.BlockType = EntityToBlockType(entity);
        vm.IsOn = entity.IsPowering() || (entity is RedstoneLamp lamp && lamp.IsPowered());

        if (entity is IPowerable powered)
        {
            vm.PowerLevel = powered.CurrentPower.Value;
        }
        else
        {
            vm.PowerLevel = 0;
        }

        if (entity is Repeater repeater)
        {
            vm.Direction = repeater.Output;
        }
        else if (entity is Torch torch)
        {
            vm.Direction = torch.Placement;
        }
        else
        {
            vm.Direction = Direction.North;
        }

        vm.Refresh();
    }

    // Maps a domain entity type to its BlockType enum value.
    private static BlockType EntityToBlockType(Entity entity)
    {
        switch (entity)
        {
            case Wire:          return BlockType.Wire;
            case Block:         return BlockType.Block;
            case Lever:         return BlockType.Lever;
            case Button:        return BlockType.Button;
            case RedstoneBlock: return BlockType.RedstoneBlock;
            case RedstoneLamp:  return BlockType.RedstoneLamp;
            case Torch:         return BlockType.Torch;
            case Repeater:      return BlockType.Repeater;
            default:            return BlockType.Air;
        }
    }

    // Creates the correct domain entity for a given block type and direction.
    private static Entity CreateEntity(BlockType type, Direction direction)
    {
        switch (type)
        {
            case BlockType.Wire:          return new Wire();
            case BlockType.Block:         return new Block();
            case BlockType.Lever:         return new Lever();
            case BlockType.Button:        return new Button();
            case BlockType.RedstoneBlock: return new RedstoneBlock();
            case BlockType.RedstoneLamp:  return new RedstoneLamp();
            case BlockType.Torch:         return new Torch(true, direction);
            case BlockType.Repeater:      return new Repeater(direction);
            default:                      return new Air();
        }
    }
}

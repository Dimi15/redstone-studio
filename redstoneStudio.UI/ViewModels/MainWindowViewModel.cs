using System;
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
    public int GridWidth  { get; } = 20;
    public int GridHeight { get; } = 15;

    public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();

    [ObservableProperty] private BlockType _selectedBlockType = BlockType.Wire;
    [ObservableProperty] private Direction _selectedDirection = Direction.North;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int _tickCount;
    [ObservableProperty] private string _statusText = "Pick a tool above, then click the grid";

    private CircuitBoard _board;
    private TickSimulator _simulator;
    private readonly DispatcherTimer _timer;

    public MainWindowViewModel()
    {
        _board     = new CircuitBoard(GridWidth, GridHeight);
        _simulator = new TickSimulator(_board);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => DoStep();

        for (int y = 0; y < GridHeight; y++)
            for (int x = 0; x < GridWidth; x++)
                Cells.Add(new CellViewModel(x, y, OnCellClicked));
    }

    // ── Simulation ───────────────────────────────────────────────────────────

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
    private void Step() => DoStep();

    [RelayCommand]
    private void Reset()
    {
        _timer.Stop();
        IsRunning  = false;
        _board     = new CircuitBoard(GridWidth, GridHeight);
        _simulator = new TickSimulator(_board);
        TickCount  = 0;
        foreach (var c in Cells) { c.BlockType = BlockType.Air; c.Refresh(); }
        StatusText = "Board cleared";
    }

    // ── Tool selection ───────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectTool(string tool)
    {
        if (Enum.TryParse<BlockType>(tool, out var bt))
        {
            SelectedBlockType = bt;
            StatusText = $"Tool: {bt}";
        }
    }

    [RelayCommand]
    private void RotateDirection()
    {
        SelectedDirection = (Direction)(((int)SelectedDirection + 1) % 4);
        StatusText = $"Direction: {SelectedDirection}";
    }

    // ── File I/O (called directly from code-behind after file picker) ─────────

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

        if (_board.Width != GridWidth || _board.Height != GridHeight)
        {
            Cells.Clear();
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                    Cells.Add(new CellViewModel(x, y, OnCellClicked));
        }

        RefreshAll();
        StatusText = $"Loaded ← {Path.GetFileName(path)}";
    }

    // ── Grid interaction ──────────────────────────────────────────────────────

    private void OnCellClicked(CellViewModel cell)
    {
        var pos      = new Position(cell.X, cell.Y);
        var existing = _board.GetEntity(pos);

        if (SelectedBlockType == BlockType.Air)
        {
            _board.RemoveEntity(pos);
        }
        else if (EntityToBlockType(existing) == SelectedBlockType)
        {
            existing.ChangeState();   // toggle lever, trigger button, cycle repeater delay, etc.
        }
        else
        {
            _board.PlaceEntity(CreateEntity(SelectedBlockType, SelectedDirection), pos);
        }

        SyncCell(cell);
        DoStep();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DoStep()
    {
        _simulator.Tick();
        TickCount = _simulator.TickCount;
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (var c in Cells) SyncCell(c);
    }

    private void SyncCell(CellViewModel vm)
    {
        var e = _board.GetEntity(new Position(vm.X, vm.Y));
        vm.BlockType  = EntityToBlockType(e);
        vm.IsOn       = e.IsPowering() || (e is RedstoneLamp l && l.IsPowered());
        vm.PowerLevel = e is IPowerable pw ? pw.CurrentPower.Value : 0;
        vm.Direction  = e is Repeater r ? r.Output
                      : e is Torch t    ? t.Placement
                      : Direction.North;
        vm.Refresh();
    }

    private static BlockType EntityToBlockType(Entity e) => e switch
    {
        Wire          => BlockType.Wire,
        Block         => BlockType.Block,
        Lever         => BlockType.Lever,
        Button        => BlockType.Button,
        RedstoneBlock => BlockType.RedstoneBlock,
        RedstoneLamp  => BlockType.RedstoneLamp,
        Torch         => BlockType.Torch,
        Repeater      => BlockType.Repeater,
        _             => BlockType.Air
    };

    private static Entity CreateEntity(BlockType type, Direction dir) => type switch
    {
        BlockType.Wire          => new Wire(),
        BlockType.Block         => new Block(),
        BlockType.Lever         => new Lever(),
        BlockType.Button        => new Button(),
        BlockType.RedstoneBlock => new RedstoneBlock(),
        BlockType.RedstoneLamp  => new RedstoneLamp(),
        BlockType.Torch         => new Torch(true, dir),
        BlockType.Repeater      => new Repeater(dir),
        _                       => new Air()
    };
}

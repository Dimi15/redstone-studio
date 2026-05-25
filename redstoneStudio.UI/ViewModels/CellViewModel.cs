using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using redstone_stone.Domain.Enums;

namespace redstone_stone.ViewModels;

/// <summary>Observable state for a single cell on the circuit grid.</summary>
public partial class CellViewModel : ViewModelBase
{
    public int X { get; }
    public int Y { get; }

    [ObservableProperty] private BlockType _blockType = BlockType.Air;
    [ObservableProperty] private Direction _direction = Direction.North;
    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private int _powerLevel;

    private readonly Action<CellViewModel> _onClicked;
    public IRelayCommand ClickCommand { get; }

    public CellViewModel(int x, int y, Action<CellViewModel> onClicked)
    {
        X = x;
        Y = y;
        _onClicked = onClicked;
        ClickCommand = new RelayCommand(() => _onClicked(this));
    }

    // ── Display symbols (Unicode) ───────────────────────────────────────────

    public string Symbol => BlockType switch
    {
        BlockType.Wire          => PowerLevel > 0 ? "·" : "·",   // dots; color carries the info
        BlockType.Lever         => IsOn  ? "│" : "╱",
        BlockType.Button        => IsOn  ? "●" : "○",
        BlockType.RedstoneBlock => "■",
        BlockType.Torch         => IsOn  ? "↑" : "↓",
        BlockType.Repeater      => Direction switch
        {
            Direction.North => "▲",
            Direction.East  => "▶",
            Direction.South => "▼",
            Direction.West  => "◀",
            _               => "▶"
        },
        BlockType.RedstoneLamp  => IsOn  ? "✦" : "✧",
        BlockType.Block         => " ",
        _                       => " "
    };

    // ── Cell background colors ──────────────────────────────────────────────

    public string CellColor => BlockType switch
    {
        BlockType.Block              => "#4A4A4A",
        BlockType.Wire when PowerLevel == 0 => "#2A0000",
        BlockType.Wire               => WireRed(PowerLevel),
        BlockType.Lever when IsOn    => "#CC9900",
        BlockType.Lever              => "#666666",
        BlockType.Button when IsOn   => "#DD4400",
        BlockType.Button             => "#774422",
        BlockType.RedstoneBlock      => "#BB0000",
        BlockType.Torch when IsOn    => "#CC4400",
        BlockType.Torch              => "#552200",
        BlockType.Repeater when IsOn => "#883300",
        BlockType.Repeater           => "#442200",
        BlockType.RedstoneLamp when IsOn  => "#DDAA00",
        BlockType.RedstoneLamp            => "#333300",
        _                                 => "#181818"   // Air
    };

    public string ForeColor => BlockType switch
    {
        BlockType.RedstoneLamp when IsOn => "#FFE860",
        BlockType.Lever when IsOn        => "#FFE860",
        BlockType.Torch when IsOn        => "#FF9944",
        BlockType.Button when IsOn       => "#FFDDAA",
        BlockType.RedstoneBlock          => "#FF6666",
        _                                => "#DDDDDD"
    };

    // Wire power gradient: dark red (power 1) → bright red-orange (power 15).
    private static string WireRed(int power)
    {
        double t = power / 15.0;
        int r = (int)(40  + t * 215);
        int g = (int)(0   + t * 60);
        return $"#{r:X2}{g:X2}00";
    }

    /// <summary>Fires property-changed for all computed display properties at once.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Symbol));
        OnPropertyChanged(nameof(CellColor));
        OnPropertyChanged(nameof(ForeColor));
    }
}

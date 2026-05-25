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

    // Delegate set by MainWindowViewModel so each cell can call back on click.
    private readonly Action<CellViewModel> _onClicked;

    public IRelayCommand ClickCommand { get; }

    public CellViewModel(int x, int y, Action<CellViewModel> onClicked)
    {
        X = x;
        Y = y;
        _onClicked = onClicked;
        ClickCommand = new RelayCommand(() => _onClicked(this));
    }

    /// <summary>Unicode symbol displayed inside the cell.</summary>
    public string Symbol => BlockType switch
    {
        BlockType.Wire         => PowerLevel > 0 ? "─" : "·",
        BlockType.Lever        => IsOn ? "⬛" : "⬜",
        BlockType.RedstoneBlock=> "R",
        BlockType.Torch        => IsOn ? "¦" : "│",
        BlockType.Repeater     => Direction switch
        {
            Direction.North => "▲",
            Direction.East  => "▶",
            Direction.South => "▼",
            Direction.West  => "◀",
            _               => "▶"
        },
        BlockType.Block        => "█",
        _                      => " "
    };

    /// <summary>Background color hex string based on entity type and power level.</summary>
    public string CellColor => BlockType switch
    {
        BlockType.Block              => "#555555",
        BlockType.Wire when PowerLevel == 0 => "#3D0000",
        BlockType.Wire               => WireColor(PowerLevel),
        BlockType.Lever when IsOn    => "#FFD700",
        BlockType.Lever              => "#888888",
        BlockType.RedstoneBlock      => "#CC0000",
        BlockType.Torch when IsOn    => "#FF6600",
        BlockType.Torch              => "#663300",
        BlockType.Repeater when IsOn => "#FF4444",
        BlockType.Repeater           => "#884444",
        _                            => "#1A1A1A"   // Air
    };

    // Wire color interpolates from dark red (power 1) to bright red (power 15).
    private static string WireColor(int power)
    {
        int r = 60 + (int)(power / 15.0 * 195);
        return $"#{r:X2}0000";
    }

    /// <summary>Raises change notifications for computed display properties after state updates.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Symbol));
        OnPropertyChanged(nameof(CellColor));
    }
}

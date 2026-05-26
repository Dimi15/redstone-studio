using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using redstone_stone.Domain.Enums;

namespace redstone_stone.ViewModels;

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
        ClickCommand = new RelayCommand(ExecuteClick);
    }

    private void ExecuteClick()
    {
        _onClicked(this);
    }

    // ── Display symbol shown inside each cell ───────────────────────────────

    public string Symbol
    {
        get
        {
            switch (BlockType)
            {
                case BlockType.Wire:          return WireSymbol(PowerLevel);
                case BlockType.Lever:         return IsOn ? "│" : "╱";
                case BlockType.Button:        return IsOn ? "●" : "○";
                case BlockType.RedstoneBlock: return "■";
                case BlockType.Torch:         return IsOn ? "↑" : "↓";
                case BlockType.Repeater:      return RepeaterSymbol(Direction);
                case BlockType.RedstoneLamp:  return IsOn ? "✦" : "✧";
                default:                      return " ";
            }
        }
    }

    // ── Cell background color ───────────────────────────────────────────────

    public string CellColor
    {
        get
        {
            if (BlockType == BlockType.Block)        return "#4A4A4A";
            if (BlockType == BlockType.RedstoneBlock) return "#BB0000";

            if (BlockType == BlockType.Wire)
            {
                if (PowerLevel == 0) return "#2A0000";
                return WireRed(PowerLevel);
            }

            if (BlockType == BlockType.Lever)       return IsOn ? "#CC9900" : "#666666";
            if (BlockType == BlockType.Button)      return IsOn ? "#DD4400" : "#774422";
            if (BlockType == BlockType.Torch)       return IsOn ? "#CC4400" : "#552200";
            if (BlockType == BlockType.Repeater)    return IsOn ? "#883300" : "#442200";
            if (BlockType == BlockType.RedstoneLamp) return IsOn ? "#DDAA00" : "#333300";

            return "#181818"; // Air
        }
    }

    // ── Text color for the symbol ───────────────────────────────────────────

    public string ForeColor
    {
        get
        {
            if (BlockType == BlockType.RedstoneLamp && IsOn) return "#FFE860";
            if (BlockType == BlockType.Lever && IsOn)        return "#FFE860";
            if (BlockType == BlockType.Torch && IsOn)        return "#FF9944";
            if (BlockType == BlockType.Button && IsOn)       return "#FFDDAA";
            if (BlockType == BlockType.RedstoneBlock)        return "#FF6666";
            return "#DDDDDD";
        }
    }

    // Notifies Avalonia to re-read the three computed display properties.
    public void Refresh()
    {
        OnPropertyChanged(nameof(Symbol));
        OnPropertyChanged(nameof(CellColor));
        OnPropertyChanged(nameof(ForeColor));
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    // Shows the signal strength as a single character:
    // 0 → "·" (no signal), 1–9 → "1"–"9", 10–15 → "A"–"F".
    private static string WireSymbol(int power)
    {
        if (power <= 0) return "·";
        if (power < 10) return power.ToString();
        return ((char)('A' + power - 10)).ToString();
    }

    // Maps a direction to the matching arrow symbol for repeaters.
    private static string RepeaterSymbol(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return "▲";
            case Direction.East:  return "▶";
            case Direction.South: return "▼";
            case Direction.West:  return "◀";
            default:              return "▶";
        }
    }

    // Wire color: dark red at low power, bright red-orange at full power (15).
    private static string WireRed(int power)
    {
        double t = power / 15.0;
        int r = (int)(40 + t * 215);
        int g = (int)(0  + t * 60);
        return $"#{r:X2}{g:X2}00";
    }
}

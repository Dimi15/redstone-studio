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

    // These three properties exist only so Avalonia can bind to them by name
    // All logic lives in the Get* methods below

    public string Symbol
    {
        get { return GetSymbol(); }
    }

    public string CellColor
    {
        get { return GetCellColor(); }
    }

    public string ForeColor
    {
        get { return GetForeColor(); }
    }

    // Notifies Avalonia to re-read the three computed display properties
    public void Refresh()
    {
        OnPropertyChanged(nameof(Symbol));
        OnPropertyChanged(nameof(CellColor));
        OnPropertyChanged(nameof(ForeColor));
    }

    // Returns the Unicode symbol to display inside the cell
    private string GetSymbol()
    {
        switch (BlockType)
        {
            case BlockType.Wire:
                return GetWireSymbol(PowerLevel);

            case BlockType.Lever:
                if (IsOn)
                {
                    return "│";
                }
                else
                {
                    return "╱";
                }

            case BlockType.Button:
                if (IsOn)
                {
                    return "●";
                }
                else
                {
                    return "○";
                }

            case BlockType.RedstoneBlock:
                return "■";

            case BlockType.Torch:
                if (IsOn)
                {
                    return "↑";
                }
                else
                {
                    return "↓";
                }

            case BlockType.Repeater:
                return GetRepeaterSymbol(Direction);

            case BlockType.RedstoneLamp:
                if (IsOn)
                {
                    return "✦";
                }
                else
                {
                    return "✧";
                }

            default:
                return " ";
        }
    }

    // Returns the background color hex string for the cell
    private string GetCellColor()
    {
        if (BlockType == BlockType.Block)
        {
            return "#4A4A4A";
        }

        if (BlockType == BlockType.RedstoneBlock)
        {
            return "#BB0000";
        }

        if (BlockType == BlockType.Wire)
        {
            if (PowerLevel == 0)
            {
                return "#2A0000";
            }

            return GetWireRed(PowerLevel);
        }

        if (BlockType == BlockType.Lever)
        {
            if (IsOn)
            {
                return "#CC9900";
            }
            else
            {
                return "#666666";
            }
        }

        if (BlockType == BlockType.Button)
        {
            if (IsOn)
            {
                return "#DD4400";
            }
            else
            {
                return "#774422";
            }
        }

        if (BlockType == BlockType.Torch)
        {
            if (IsOn)
            {
                return "#CC4400";
            }
            else
            {
                return "#552200";
            }
        }

        if (BlockType == BlockType.Repeater)
        {
            if (IsOn)
            {
                return "#883300";
            }
            else
            {
                return "#442200";
            }
        }

        if (BlockType == BlockType.RedstoneLamp)
        {
            if (IsOn)
            {
                return "#DDAA00";
            }
            else
            {
                return "#333300";
            }
        }

        return "#181818"; // Air
    }

    // Returns the foreground (text) color hex string for the cell symbol
    private string GetForeColor()
    {
        if (BlockType == BlockType.RedstoneLamp)
        {
            if (IsOn)
            {
                return "#FFE860";
            }
        }

        if (BlockType == BlockType.Lever)
        {
            if (IsOn)
            {
                return "#FFE860";
            }
        }

        if (BlockType == BlockType.Torch)
        {
            if (IsOn)
            {
                return "#FF9944";
            }
        }

        if (BlockType == BlockType.Button)
        {
            if (IsOn)
            {
                return "#FFDDAA";
            }
        }

        if (BlockType == BlockType.RedstoneBlock)
        {
            return "#FF6666";
        }

        return "#DDDDDD";
    }

    // Shows the signal strength as a single character:
    // 0 → "·" (no signal), 1–9 → "1"–"9", 10–15 → "A"–"F"
    private static string GetWireSymbol(int power)
    {
        if (power <= 0)
        {
            return "·";
        }

        if (power < 10)
        {
            return power.ToString();
        }

        int charOffset = power - 10;
        char hexChar = (char)('A' + charOffset);
        return hexChar.ToString();
    }

    // Maps a direction to the matching arrow symbol for repeaters
    private static string GetRepeaterSymbol(Direction dir)
    {
        switch (dir)
        {
            case Direction.North:
                return "▲";
            case Direction.East:
                return "▶";
            case Direction.South:
                return "▼";
            case Direction.West:
                return "◀";
            default:
                return "▶";
        }
    }

    // Wire color: dark red at low power, bright red-orange at full power (15)
    private static string GetWireRed(int power)
    {
        double ratio = power / 15.0;
        int red = (int)(40 + ratio * 215);
        int green = (int)(0 + ratio * 60);
        string redHex = red.ToString("X2");
        string greenHex = green.ToString("X2");
        return "#" + redHex + greenHex + "00";
    }
}
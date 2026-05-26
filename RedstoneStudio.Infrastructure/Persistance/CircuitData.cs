using System.Collections.Generic;

namespace redstone_stone.Infrastructure.Persistance;

/// <summary>Top-level DTO that represents a saved circuit file.</summary>
public class CircuitData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<CellData> Cells { get; set; } = new();
}

/// <summary>Serializable snapshot of a single non-Air cell.</summary>
public class CellData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = "Air";
    public string? Direction { get; set; } // Torch / Repeater facing
    public bool? IsOn { get; set; } // Lever state; Torch initial state
    public int? Delay { get; set; } // Repeater tick delay
    public bool? Vertical { get; set; } // Torch: floor/wall mount
}
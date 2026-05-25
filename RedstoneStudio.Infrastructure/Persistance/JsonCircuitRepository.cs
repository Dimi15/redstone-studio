using System.Text.Json;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Infrastructure.Persistance;

// Plain data containers used for JSON serialization — not domain entities.
public class CircuitData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<CellData> Cells { get; set; } = new();
}

public class CellData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = "Air";
    public string? Direction { get; set; }
    public bool? IsOn { get; set; }
    public int? Delay { get; set; }
    public bool? Vertical { get; set; }
}

// Handles reading and writing circuit files in JSON format.
public static class JsonCircuitRepository
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    /// <summary>Serializes the circuit board to a JSON file at the given path.</summary>
    public static async Task SaveAsync(CircuitBoard board, string path)
    {
        var data = new CircuitData { Width = board.Width, Height = board.Height };

        foreach (var pos in board.AllPositions())
        {
            var cell = EntityToCellData(board.GetEntity(pos), pos);
            if (cell != null) data.Cells.Add(cell);
        }

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _options));
    }

    /// <summary>Deserializes a circuit board from a JSON file.</summary>
    public static async Task<CircuitBoard> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var data = JsonSerializer.Deserialize<CircuitData>(json)
                   ?? throw new InvalidDataException("Could not read circuit file.");

        var board = new CircuitBoard(data.Width, data.Height);

        foreach (var cell in data.Cells)
        {
            var entity = CellDataToEntity(cell);
            if (entity != null)
                board.PlaceEntity(entity, new Position(cell.X, cell.Y));
        }

        return board;
    }

    // Maps a domain entity to its serializable DTO. Returns null for Air (not stored).
    private static CellData? EntityToCellData(Entity entity, Position pos) => entity switch
    {
        Wire         => new CellData { X = pos.X, Y = pos.Y, Type = "Wire" },
        Block        => new CellData { X = pos.X, Y = pos.Y, Type = "Block" },
        Lever l      => new CellData { X = pos.X, Y = pos.Y, Type = "Lever",        IsOn = l.IsOn },
        RedstoneBlock=> new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneBlock" },
        Torch t      => new CellData { X = pos.X, Y = pos.Y, Type = "Torch",
                            Direction = t.Placement.ToString(), Vertical = t.IsVertical, IsOn = t.IsOn },
        Repeater r   => new CellData { X = pos.X, Y = pos.Y, Type = "Repeater",
                            Direction = r.Output.ToString(), Delay = r.Delay },
        _            => null
    };

    // Reconstructs a domain entity from its serializable DTO.
    private static Entity? CellDataToEntity(CellData cell) => cell.Type switch
    {
        "Wire"          => new Wire(),
        "Block"         => new Block(),
        "Lever"         => new Lever(),
        "RedstoneBlock" => new RedstoneBlock(),
        "Torch"         => new Torch(
                               cell.Vertical ?? true,
                               Enum.Parse<Direction>(cell.Direction ?? "North")),
        "Repeater"      => new Repeater(
                               Enum.Parse<Direction>(cell.Direction ?? "North")),
        _               => null
    };
}

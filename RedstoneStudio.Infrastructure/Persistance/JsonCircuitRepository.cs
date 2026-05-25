using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Infrastructure.Persistance;

/// <summary>Reads and writes circuit boards as indented JSON files.</summary>
public static class JsonCircuitRepository
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    /// <summary>Serializes the board to a JSON file. Only non-Air cells are stored.</summary>
    public static async Task SaveAsync(CircuitBoard board, string path)
    {
        var data = new CircuitData { Width = board.Width, Height = board.Height };

        foreach (var pos in board.AllPositions())
        {
            var cell = ToCellData(board.GetEntity(pos), pos);
            if (cell != null) data.Cells.Add(cell);
        }

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _options));
    }

    /// <summary>Deserializes a board from a JSON file.</summary>
    public static async Task<CircuitBoard> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var data = JsonSerializer.Deserialize<CircuitData>(json)
                   ?? throw new InvalidDataException("Could not read circuit file.");

        var board = new CircuitBoard(data.Width, data.Height);

        foreach (var cell in data.Cells)
        {
            var entity = ToEntity(cell);
            if (entity != null) board.PlaceEntity(entity, new Position(cell.X, cell.Y));
        }

        return board;
    }

    private static CellData? ToCellData(Entity entity, Position pos) => entity switch
    {
        Wire          => new CellData { X = pos.X, Y = pos.Y, Type = "Wire" },
        Block         => new CellData { X = pos.X, Y = pos.Y, Type = "Block" },
        Lever l       => new CellData { X = pos.X, Y = pos.Y, Type = "Lever",        IsOn = l.IsOn },
        Button        => new CellData { X = pos.X, Y = pos.Y, Type = "Button" },
        RedstoneBlock => new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneBlock" },
        RedstoneLamp  => new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneLamp" },
        Torch t       => new CellData { X = pos.X, Y = pos.Y, Type = "Torch",
                             Direction = t.Placement.ToString(), Vertical = t.IsVertical },
        Repeater r    => new CellData { X = pos.X, Y = pos.Y, Type = "Repeater",
                             Direction = r.Output.ToString(), Delay = r.Delay },
        _             => null
    };

    private static Entity? ToEntity(CellData cell) => cell.Type switch
    {
        "Wire"          => new Wire(),
        "Block"         => new Block(),
        "Lever"         => new Lever(),
        "Button"        => new Button(),
        "RedstoneBlock" => new RedstoneBlock(),
        "RedstoneLamp"  => new RedstoneLamp(),
        "Torch"         => new Torch(cell.Vertical ?? true,
                               Enum.Parse<Direction>(cell.Direction ?? "North")),
        "Repeater"      => new Repeater(Enum.Parse<Direction>(cell.Direction ?? "North")),
        _               => null
    };
}

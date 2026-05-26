using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Infrastructure.Persistance;

// Reads and writes circuit boards as indented JSON files.
// Snapshot() and FromSnapshot() are also used internally for undo/redo history.
public static class JsonCircuitRepository
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true };

    // Serializes the board to a JSON file. Only non-Air cells are stored.
    public static async Task SaveAsync(CircuitBoard board, string path)
    {
        var data = Snapshot(board);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _options));
    }

    // Deserializes a board from a JSON file.
    public static async Task<CircuitBoard> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var data = JsonSerializer.Deserialize<CircuitData>(json)
                   ?? throw new InvalidDataException("Could not read circuit file.");
        return FromSnapshot(data);
    }

    // Creates a plain-data snapshot of the board (used for undo/redo).
    public static CircuitData Snapshot(CircuitBoard board)
    {
        var data = new CircuitData { Width = board.Width, Height = board.Height };
        foreach (var pos in board.AllPositions())
        {
            var cell = ToCellData(board.GetEntity(pos), pos);
            if (cell != null)
            {
                data.Cells.Add(cell);
            }
        }

        return data;
    }

    // Rebuilds a CircuitBoard from a plain-data snapshot (used for undo/redo and load).
    public static CircuitBoard FromSnapshot(CircuitData data)
    {
        var board = new CircuitBoard(data.Width, data.Height);
        foreach (var cell in data.Cells)
        {
            var entity = ToEntity(cell);
            if (entity != null)
            {
                board.PlaceEntity(entity, new Position(cell.X, cell.Y));
            }
        }

        return board;
    }

    private static CellData? ToCellData(Entity entity, Position pos)
    {
        if (entity is Wire)
        {
            return new CellData { X = pos.X, Y = pos.Y, Type = "Wire" };
        }

        if (entity is Block)
        {
            return new CellData { X = pos.X, Y = pos.Y, Type = "Block" };
        }

        if (entity is Lever l)
        {
            return new CellData { X = pos.X, Y = pos.Y, Type = "Lever", IsOn = l.IsOn };
        }

        if (entity is Button)
        {
            return new CellData { X = pos.X, Y = pos.Y, Type = "Button" };
        }

        if (entity is RedstoneBlock)
        {
            return new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneBlock" };
        }

        if (entity is RedstoneLamp)
        {
            return new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneLamp" };
        }

        if (entity is Torch t)
        {
            return new CellData
            {
                X = pos.X, Y = pos.Y, Type = "Torch",
                Direction = t.Placement.ToString(), Vertical = t.IsVertical
            };
        }

        if (entity is Repeater r)
        {
            return new CellData
            {
                X = pos.X, Y = pos.Y, Type = "Repeater",
                Direction = r.Output.ToString(), Delay = r.Delay
            };
        }

        return null;
    }

    private static Entity? ToEntity(CellData cell)
    {
        switch (cell.Type)
        {
            case "Wire": return new Wire();
            case "Block": return new Block();
            case "Lever": return new Lever(cell.IsOn ?? false);
            case "Button": return new Button();
            case "RedstoneBlock": return new RedstoneBlock();
            case "RedstoneLamp": return new RedstoneLamp();
            case "Torch":
                return new Torch(
                    cell.Vertical ?? true,
                    Enum.Parse<Direction>(cell.Direction ?? "North")
                );
            case "Repeater":
                return new Repeater(
                    Enum.Parse<Direction>(cell.Direction ?? "North"),
                    cell.Delay ?? 1
                );
            default: return null;
        }
    }
}
using System.Collections.Generic;
using redstone_stone.Application.Grid;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application;

// In-memory snapshot of a CircuitBoard. Used by the undo/redo history in the UI.
public class BoardSnapshot
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<CellSnapshot> Cells { get; set; } = new List<CellSnapshot>();

    // Captures the current state of the board as a plain-data snapshot.
    public static BoardSnapshot Capture(CircuitBoard board)
    {
        BoardSnapshot snapshot = new BoardSnapshot { Width = board.Width, Height = board.Height };

        foreach (Position pos in board.AllPositions())
        {
            CellSnapshot? cell = CellSnapshotFrom(board.GetEntity(pos), pos);
            if (cell != null)
            {
                snapshot.Cells.Add(cell);
            }
        }

        return snapshot;
    }

    // Rebuilds a CircuitBoard from a snapshot.
    public static CircuitBoard Restore(BoardSnapshot snapshot)
    {
        CircuitBoard board = new CircuitBoard(snapshot.Width, snapshot.Height);

        foreach (CellSnapshot cell in snapshot.Cells)
        {
            Entity? entity = EntityFrom(cell);
            if (entity != null)
            {
                board.PlaceEntity(entity, new Position(cell.X, cell.Y));
            }
        }

        return board;
    }

    private static CellSnapshot? CellSnapshotFrom(Entity entity, Position pos)
    {
        if (entity is Wire)
        {
            return new CellSnapshot { X = pos.X, Y = pos.Y, Type = "Wire" };
        }
        
        if (entity is Block)
        {
            return new CellSnapshot { X = pos.X, Y = pos.Y, Type = "Block" };
        }

        if (entity is Lever)
        {
            Lever lever = (Lever)entity;
            return new CellSnapshot { X = pos.X, Y = pos.Y, Type = "Lever", IsOn = lever.IsOn };
        }

        if (entity is Button)
        {
            return new CellSnapshot { X = pos.X, Y = pos.Y, Type = "Button" };
        }

        if (entity is RedstoneBlock)
        {
            return new CellSnapshot { X = pos.X, Y = pos.Y, Type = "RedstoneBlock" };
        }

        if (entity is RedstoneLamp)
        {
            return new CellSnapshot { X = pos.X, Y = pos.Y, Type = "RedstoneLamp" };
        }

        if (entity is Torch)
        {
            Torch torch = (Torch)entity;
            return new CellSnapshot
            {
                X = pos.X, Y = pos.Y, Type = "Torch",
                Direction = torch.Placement.ToString(), Vertical = torch.IsVertical
            };
        }

        if (entity is Repeater)
        {
            Repeater repeater = (Repeater)entity;
            return new CellSnapshot
            {
                X = pos.X, Y = pos.Y, Type = "Repeater",
                Direction = repeater.Output.ToString(), Delay = repeater.Delay
            };
        }

        return null;
    }

    private static Entity? EntityFrom(CellSnapshot cell)
    {
        switch (cell.Type)
        {
            case "Wire":
                return new Wire();

            case "Block":
                return new Block();

            case "Lever":
                bool leverIsOn = false;
                if (cell.IsOn != null)
                {
                    leverIsOn = cell.IsOn.Value;
                }

                return new Lever(leverIsOn);

            case "Button":
                return new Button();

            case "RedstoneBlock":
                return new RedstoneBlock();

            case "RedstoneLamp":
                return new RedstoneLamp();

            case "Torch":
                bool torchVertical = true;
                if (cell.Vertical != null)
                {
                    torchVertical = cell.Vertical.Value;
                }

                string torchDirection = "North";
                if (cell.Direction != null)
                {
                    torchDirection = cell.Direction;
                }

                return new Torch(torchVertical, Enum.Parse<Direction>(torchDirection));

            case "Repeater":
                string repeaterDirection = "North";
                if (cell.Direction != null)
                {
                    repeaterDirection = cell.Direction;
                }

                int repeaterDelay = 1;
                if (cell.Delay != null)
                {
                    repeaterDelay = cell.Delay.Value;
                }

                return new Repeater(Enum.Parse<Direction>(repeaterDirection), repeaterDelay);

            default:
                return null;
        }
    }
}

// Plain-data description of a single non-Air cell in a snapshot.
public class CellSnapshot
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = "Air";
    public string? Direction { get; set; }
    public bool? IsOn { get; set; }
    public int? Delay { get; set; }
    public bool? Vertical { get; set; }
}
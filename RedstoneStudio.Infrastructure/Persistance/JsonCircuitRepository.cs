using System.Text.Json;
using System.IO;
using System;


namespace redstone_stone.Infrastructure.Persistance;

// Something to save the circuits, in this case serialize/deserialize json file...
public class JsonCircuitRepository
{
    public static void Save(Entity[,] circuitBoard, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(circuitBoard));
    }
}
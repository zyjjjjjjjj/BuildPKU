using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public static class BuildingDatabase
{
    private const string JsonPath = "res://buildings/BuildingsID.json";
    private static List<BuildingData> _buildings;
    private static Dictionary<string, BuildingData> _buildingMap;

    public static List<BuildingData> GetAll()
    {
        EnsureLoaded();
        return _buildings;
    }

    public static BuildingData GetById(string id)
    {
        EnsureLoaded();

        if (_buildingMap.TryGetValue(id, out var data))
            return data;

        GD.PrintErr($"找不到建筑配置: {id}");
        return null;
    }

    private static void EnsureLoaded()
    {
        if (_buildings != null)
            return;

        if (!FileAccess.FileExists(JsonPath))
        {
            GD.PrintErr($"找不到建筑配置文件: {JsonPath}");
            _buildings = new List<BuildingData>();
            _buildingMap = new Dictionary<string, BuildingData>();
            return;
        }

        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        string jsonString = file.GetAsText();

        _buildings = JsonSerializer.Deserialize<List<BuildingData>>(jsonString)
                     ?? new List<BuildingData>();

        _buildingMap = new Dictionary<string, BuildingData>();

        foreach (var data in _buildings)
        {
            _buildingMap[data.Id] = data;
        }
    }
}

public class BuildingData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string IconPath { get; set; }
    public int Cost { get; set; }
}
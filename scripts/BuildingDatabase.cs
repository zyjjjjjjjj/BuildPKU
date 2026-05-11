using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public static class BuildingDatabase
{
    private const string JsonPath = "res://buildings/BuildingsID.json";
    private static Dictionary<string, BuildingData> _allBuildingDict;

    public static Dictionary<string, BuildingData> GetAll()
    {
        EnsureLoaded();
        return _allBuildingDict;
    }

    public static BuildingData GetById(string id)
    {
        EnsureLoaded();

        if (_allBuildingDict.TryGetValue(id, out var data))
            return data;

        GD.PrintErr($"找不到建筑配置: {id}");
        return null;
    }

    private static void EnsureLoaded()
    {
        if (_allBuildingDict != null)
            return;

        if (!FileAccess.FileExists(JsonPath))
        {
            GD.PrintErr($"找不到建筑配置文件: {JsonPath}");
            _allBuildingDict = new Dictionary<string, BuildingData>();
            return;
        }

        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        string jsonString = file.GetAsText();

        var list = JsonSerializer.Deserialize<List<BuildingData>>(jsonString)
                     ?? new List<BuildingData>();

        _allBuildingDict = new Dictionary<string, BuildingData>();

        foreach (var data in list)
        {
            _allBuildingDict[data.Id] = data;
        }
    }
}
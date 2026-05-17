using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public class BuildingData
{
    public string Id{get;set;}
    public string Name{get;set;}
    public List<string> Tags { get; set; }
    public string IconPath => "res://icon.svg";
    public string ScenePath => $"res://buildings/{Id}.tscn";
    public float Research { get; set; }
    public float Education { get; set; }
    public float Environment { get; set; }
    public float Cost { get; set; }
    public float Benefit { get; set; }
    public float Population { get; set; }
    public bool Destroyable { get; set; } = true;
    public int Limit { get; set; } = -1;
    public Dictionary<string,float> UpgradeCost { get; set; } = new Dictionary<string, float>();
}

public static class BuildingDatabase
{
    private const string JsonPath = "res://json_data/Buildings.json";
    private static Dictionary<string, BuildingData> _allBuildingDict;

    public static Dictionary<string, BuildingData> GetAll()
    {
        EnsureLoaded();
        return _allBuildingDict;
    }
    
    public static List<string> GetAllIds()
    {
        EnsureLoaded();
        return new List<string>(_allBuildingDict.Keys);
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
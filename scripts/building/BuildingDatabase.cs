using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public class BuildingData
{
    public string Id{get;set;}
    public string Name{get;set;}
    public List<string> Tags { get; set; } = new List<string>();
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
    public bool InitiallyAvailable { get; set; } = false;
    public Dictionary<string,float> UpgradeCost { get; set; } = new Dictionary<string, float>();
}

public static class BuildingDatabase
{
    private const string JsonPath = "res://json_data/Buildings.json";
    private const string InitialBuildingJsonPath = "res://json_data/Initial_Buildings.json";
    private static Dictionary<string, BuildingData> _allBuildingDict;
    private static List<string> _initialBuildingIds = [];

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

    public static List<string> GetInitiallyAvailableIds()
    {
        EnsureLoaded();
        return _initialBuildingIds;
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
        if (_allBuildingDict != null && _initialBuildingIds != null)
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
        
        if(!FileAccess.FileExists(InitialBuildingJsonPath))
        {
            GD.PrintErr($"找不到初始建筑配置文件: {InitialBuildingJsonPath}");
            _initialBuildingIds = [];
            return;
        }
        
        using var initialFile = FileAccess.Open(InitialBuildingJsonPath, FileAccess.ModeFlags.Read);
        string initialJsonString = initialFile.GetAsText();
        _initialBuildingIds = JsonSerializer.Deserialize<List<string>>(initialJsonString) ?? [];

        CheckIllegal();
    }
    
    private static void CheckIllegal()
    {
        foreach (var building in _initialBuildingIds)
        {
            if(!_allBuildingDict.ContainsKey(building))
                GD.PrintErr($"初始建筑配置 {building} 在建筑列表中不存在");
        }
        
        foreach (var kv in _allBuildingDict)
        {
            var id = kv.Key;
            var data = kv.Value;
            if (string.IsNullOrEmpty(data.Id))
                GD.PrintErr($"建筑配置 {id} 的 Id 不能为空");
            if (string.IsNullOrEmpty(data.Name))
                GD.PrintErr($"建筑配置 {id} 的 Name 不能为空");
            if (!ResourceLoader.Exists(data.ScenePath))
            {
                GD.PushWarning($"建筑配置 {id} 的 ScenePath 不存在: {data.ScenePath}, 从列表中移除");
            }
            if (data.Tags != null)
            {
                foreach(var tag in data.Tags)
                {
                    if (string.IsNullOrEmpty(tag))
                    {
                        GD.PrintErr($"建筑配置 {id} 的 Tags 中存在空字符串");
                        continue;
                    }
                    if(_allBuildingDict.ContainsKey(tag))
                        GD.PrintErr($"建筑{id}的Tag{tag}与某个其他建筑Id同名");
                    
                }
            }
        }
    }
    
    public static bool IsId(string id)
    {
        EnsureLoaded();
        return _allBuildingDict.ContainsKey(id);
    }
}

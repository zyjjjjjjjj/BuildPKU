using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public class BuildingData
{
    public string Id{get;init;}
    public string Name{get;init;}
    public List<string> Tags { get; init; } = new List<string>();
    public string IconPath => "res://icon.svg";
    public string ScenePath => $"res://buildings/{Id}.tscn";
    public float Research { get; init; }
    public float Education { get; init; }
    public float Environment { get; init; }
    public float Cost { get; init; }
    public float Benefit { get; init; }
    public float Population { get; init; }
    public bool Destroyable { get; init; } = true;
    public int Limit { get; init; } = -1;
    public Dictionary<string,float> UpgradeCost { get; init; } = new Dictionary<string, float>();
    
    public BuildingData Clone()
    {
        return new BuildingData
        {
            Id = Id,
            Name = Name,
            Tags = [..Tags],
            Research = Research,
            Education = Education,
            Environment = Environment,
            Cost = Cost,
            Benefit = Benefit,
            Population = Population,
            Destroyable = Destroyable,
            Limit = Limit,
            UpgradeCost = new Dictionary<string, float>(UpgradeCost)
        };
    }
}

public static class BuildingDatabase
{
    private const string JsonPath = "res://json_data/Buildings.json";
    private const string RegionJsonPath = "res://json_data/Regions.json";
    private const string InitialBuildingJsonPath = "res://json_data/Initial_Buildings.json";
    private const string TagsDictJsonPath = "res://json_data/Tags_Dictionary.json";
    private static Dictionary<string, BuildingData> _allBuildingDict;
    private static Dictionary<string, BuildingData> _allRegionDict;
    private static List<string> _initialBuildingIds;
    private static Dictionary<string, string> _tagsDict;

    public static IReadOnlyDictionary<string, BuildingData> GetAll()
    {
        EnsureLoaded();
        return _allBuildingDict;
    }
    
    public static IReadOnlyList<string> GetAllIds()
    {
        EnsureLoaded();
        return new List<string>(_allBuildingDict.Keys);
    }

    public static IReadOnlyList<string> GetInitiallyAvailableIds()
    {
        EnsureLoaded();
        return _initialBuildingIds;
    }

    public static BuildingData GetById(string id)
    {
        EnsureLoaded();

        if (_allBuildingDict.TryGetValue(id, out var data))
            return data.Clone();
        
        if(_allRegionDict.TryGetValue(id, out var regionData))
            return regionData.Clone();

        GD.PrintErr($"找不到建筑配置: {id}");
        return null;
    }
    
    public static string GetNameById(string id)
    {
        EnsureLoaded();
        if (_allBuildingDict.TryGetValue(id, out var data))
            return data.Name;
        if(_allRegionDict.TryGetValue(id, out var regionData))
            return regionData.Name;
        if(_tagsDict.TryGetValue(id, out var tagName))
            return tagName;
        GD.PrintErr($"找不到建筑或标签配置: {id}");
        return null;
    }

    private static void EnsureLoaded()
    {
        if (_allBuildingDict != null && _initialBuildingIds != null && _allRegionDict != null && _tagsDict != null)
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
            _allBuildingDict[data.Id] = data;
        
        if(!FileAccess.FileExists(InitialBuildingJsonPath))
        {
            GD.PrintErr($"找不到初始建筑配置文件: {InitialBuildingJsonPath}");
            _initialBuildingIds = [];
            return;
        }
        using var initialFile = FileAccess.Open(InitialBuildingJsonPath, FileAccess.ModeFlags.Read);
        string initialJsonString = initialFile.GetAsText();
        _initialBuildingIds = JsonSerializer.Deserialize<List<string>>(initialJsonString) ?? [];
        
        if(!FileAccess.FileExists(RegionJsonPath))
        {
            GD.PrintErr($"找不到区域配置文件: {RegionJsonPath}");
            _allRegionDict = new Dictionary<string, BuildingData>();
            return;
        }
        using var regionFile = FileAccess.Open(RegionJsonPath, FileAccess.ModeFlags.Read);
        string regionJsonString = regionFile.GetAsText();
        var regionList = JsonSerializer.Deserialize<List<BuildingData>>(regionJsonString)
                         ?? new List<BuildingData>();
        _allRegionDict = new Dictionary<string, BuildingData>();
        foreach (var data in regionList)
            _allRegionDict[data.Id] = data;
        
        if(!FileAccess.FileExists(TagsDictJsonPath))
        {
            GD.PrintErr($"找不到标签字典配置文件: {TagsDictJsonPath}");
            _tagsDict = new Dictionary<string, string>();
            return;
        }
        using var tagsDictFile = FileAccess.Open(TagsDictJsonPath, FileAccess.ModeFlags.Read);
        string tagsDictJsonString = tagsDictFile.GetAsText();
        _tagsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(tagsDictJsonString)
                    ?? new Dictionary<string, string>();

        AddAutoTag();

        CheckIllegal();
    }
    
    private static void AddAutoTag()
    {
        foreach (var kv in _allBuildingDict)
        {
            var id = kv.Key;
            var data = kv.Value;
            if(data.Benefit > 0)
                data.Tags.Add("auto_pos_ben");
            else if(data.Benefit < 0)                
                data.Tags.Add("auto_neg_ben");
        }
    }
    
    private static void CheckIllegal()
    {
        foreach (var building in _initialBuildingIds)
        {
            if(!_allBuildingDict.ContainsKey(building))
                GD.PushWarning($"初始建筑配置 {building} 在建筑列表中不存在");
        }
        
        foreach (var kv in _allBuildingDict)
        {
            var id = kv.Key;
            var data = kv.Value;
            if (string.IsNullOrEmpty(data.Id))
                GD.PushWarning($"建筑配置 {id} 的 Id 不能为空");
            if (string.IsNullOrEmpty(data.Name))
                GD.PushWarning($"建筑配置 {id} 的 Name 不能为空");
            if (!ResourceLoader.Exists(data.ScenePath))
            {
                GD.PushWarning($"建筑配置 {id} 的 ScenePath 不存在: {data.ScenePath}");
            }
            if (data.Tags != null)
            {
                foreach(var tag in data.Tags)
                {
                    if (string.IsNullOrEmpty(tag))
                    {
                        GD.PushWarning($"建筑配置 {id} 的 Tags 中存在空字符串");
                        continue;
                    }
                    if(_allBuildingDict.ContainsKey(tag))
                        GD.PushWarning($"建筑{id}的Tag{tag}与某个其他建筑Id同名");
                    if(!_tagsDict.ContainsKey(tag))
                        GD.PushWarning($"建筑 {id} 的 Tag {tag} 在标签字典中没有对应的名称");
                    
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

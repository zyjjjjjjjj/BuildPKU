using System;
using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;


public class BuffData
{
    public string Id { get; init; }
    public string Description { get; init; }

    public BuffData Clone()
    {
        return new BuffData
        {
            Id = Id,
            Description = Description
        };
    }
}

public static class BuffDatabase
{
    private const string JsonPath = "res://json_data/Buffs.json";
    private static Dictionary<string, BuffData> _buffs;

    public static BuffData GetById(string id)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(id))
            return null;

        if (_buffs.TryGetValue(id, out var data))
            return data.Clone();

        GD.PrintErr($"找不到 Buff 配置: {id}");
        return null;
    }
    
    public static bool Exists(string id)
    {
        EnsureLoaded();
        return _buffs.ContainsKey(id);
    }

    public static void EnsureLoaded()
    {
        if (_buffs != null)
            return;

        if (!FileAccess.FileExists(JsonPath))
        {
            GD.PrintErr($"找不到 Buff 配置文件: {JsonPath}");
            _buffs = [];
            return;
        }

        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        string jsonString = file.GetAsText();

        var list = JsonSerializer.Deserialize<List<BuffData>>(jsonString) ?? [];
        _buffs = [];

        foreach (var buff in list)
            _buffs[buff.Id] = buff;

        CheckIllegal();
    }

    private static void CheckIllegal()
    {
    }
}
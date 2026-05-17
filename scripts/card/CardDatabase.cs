using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;
using System;

public class BuffArgs
{
    public int StartYear { get; set; } = 0;
    public int DurationYears { get; set; } = -1;
    public string Target { get; set; } = String.Empty;
    public int Value { get; set; } = 0;
    public string SrcBuildingIdOrTag { get; set; } = String.Empty;
    public string DestBuildingIdOrTag { get; set; } = String.Empty;
}

public class CardData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CardType { get; set; }
    public string Rarity { get; set; }
    public int MinYear { get; set; }
    public bool Repeatable { get; set; }
    public Dictionary<string, BuffArgs> Buffs { get; set; }
}

public static class CardDatabase
{
    private const string JsonPath = "res://json_data/Cards.json";
    private static List<CardData> _allCards;

    public static List<CardData> GetAll()
    {
        EnsureLoaded();
        return _allCards;
    }

    private static void EnsureLoaded()
    {
        if (_allCards != null)
            return;

        if (!FileAccess.FileExists(JsonPath))
        {
            GD.PrintErr($"找不到卡牌配置文件: {JsonPath}");
            _allCards = [];
            return;
        }

        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        string jsonString = file.GetAsText();

        try
        {
            _allCards = JsonSerializer.Deserialize<List<CardData>>(jsonString) ?? [];
        }
        catch (Exception e)
        {
            GD.PrintErr($"卡牌配置解析失败: {e.Message}");
            _allCards = [];
        }
    }
}
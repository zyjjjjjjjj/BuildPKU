using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public class BuffArgs
{
    public int StartYearOffset { get; init; } = 0;
    public int DurationYears { get; init; } = -1;
    public string Target { get; init; } = String.Empty;
    public int Value { get; init; } = 0;
    public string SrcBuildingIdOrTag { get; init; } = String.Empty;
    public string DestBuildingIdOrTag { get; init; } = String.Empty;
}

public class CardData
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string CardType { get; init; }
    public string Rarity { get; init; }
    public int MinYear { get; init; }
    public bool Repeatable { get; init; }
    public Dictionary<string, BuffArgs> Buffs { get; init; }
}

public static class CardDatabase
{
    private const string JsonPath = "res://json_data/Cards.json";
    private static List<CardData> _allCards;

    public static IReadOnlyList<CardData> GetAll()
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

        CheckIllegal();
    }
    
    private static void CheckIllegal()
    {
        foreach (var card in _allCards)
        {
            foreach (var buffId in card.Buffs.Keys.Where(buffId => !BuffDatabase.Exists(Regex.Replace(buffId, @"_\d+$", ""))))
            {
                GD.PrintErr($"卡牌 {card.Id} 包含未知 Buff: {buffId}");
            }
        }
    }
}
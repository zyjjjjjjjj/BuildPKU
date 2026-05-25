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
    public List<string> Targets { get; init; } = [];
    public float Value { get; init; } = 0f;
    public string SrcBuildingIdOrTag { get; init; } = String.Empty;
    public string DestBuildingIdOrTag { get; init; } = String.Empty;
    public string ConditionStat { get; init; } = String.Empty;
    public string ConditionOp { get; init; } = String.Empty;
    public float ConditionValue { get; init; } = 0f;
    public int Limit { get; init; } = -1;
    
}

public class Event
{
    public int Year { get; init; }
    public List<string> CardIds { get; init; } = new List<string>();
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
    private const string AutoCardsJsonPath = "res://json_data/Auto_Cards.json";
    private const string JsonPath = "res://json_data/Cards.json";
    private static Dictionary<string,CardData> _allCards;
    private static Dictionary<int,Event> _events;

    public static IReadOnlyList<CardData> GetAll()
    {
        EnsureLoaded();
        return _allCards.Values.ToList();
    }

    public static void EnsureLoaded()
    {
        if (_allCards != null && _events != null)
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
            var allCardsList = JsonSerializer.Deserialize<List<CardData>>(jsonString) ?? [];
            _allCards = new Dictionary<string,CardData>();
            foreach(var card in allCardsList)
                _allCards.Add(card.Id, card);
        }
        catch (Exception e)
        {
            GD.PrintErr($"卡牌配置解析失败: {e.Message}");
            _allCards = [];
        }
        
        if(!FileAccess.FileExists(AutoCardsJsonPath))
        {
            GD.PrintErr($"找不到自动卡牌配置文件: {AutoCardsJsonPath}");
            _events = [];
            return;
        }
        using var eventFile = FileAccess.Open(AutoCardsJsonPath, FileAccess.ModeFlags.Read);
        string eventJsonString = eventFile.GetAsText();
        try
        {
            var eventsList = JsonSerializer.Deserialize<List<Event>>(eventJsonString) ?? [];
            _events = new Dictionary<int, Event>();
            foreach (var e in eventsList)
                _events[e.Year] = e;
        }
        catch (Exception e)
        {
            GD.PrintErr($"自动卡牌配置解析失败: {e.Message}");
            _events = [];
        }
        
        CheckIllegal();
    }
    
    private static void CheckIllegal()
    {
        foreach (var card in _allCards.Values)
        {
            foreach (var buffId in card.Buffs.Keys.Where(buffId => !BuffDatabase.Exists(Regex.Replace(buffId, @"_\d+$", ""))))
            {
                GD.PrintErr($"卡牌 {card.Id} 包含未知 Buff: {buffId}");
            }
        }

        foreach (var p in _events)
        {
            foreach (var card in p.Value.CardIds)
            {
                if(!_allCards.ContainsKey(card))
                    GD.PrintErr($"事件 {p.Value.Year} 包含未知卡牌: {card}");
            }
        }
    }

    public static Event GetEvent(int year)
    {
        EnsureLoaded();
        return _events.TryGetValue(year,out var evt) ? evt : null;
    }

    public static CardData GetCardDataById(string cardId)
    {
        EnsureLoaded();
        return _allCards.TryGetValue(cardId, out var card) ? card : null;
    }
}

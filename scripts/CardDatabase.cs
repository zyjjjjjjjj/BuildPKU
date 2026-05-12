using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

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

        _allCards = JsonSerializer.Deserialize<List<CardData>>(jsonString) ?? [];
    }
}
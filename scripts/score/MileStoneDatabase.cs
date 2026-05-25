using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public class MileStoneData
{
	public int Year { get; init; }
	public int TargetScore { get; init; }
	
	public MileStoneData Clone()
	{
		return new MileStoneData
		{
			Year = Year,
			TargetScore = TargetScore
		};
	}
}

public static class MileStoneDatabase
{
	private const string JsonPath = "res://json_data/Milestones.json";
	private static List<MileStoneData> _mileStonesList;

	public static MileStoneData GetNext(int year)// may return null
	{
		EnsureLoaded();
		foreach (var mileStone in _mileStonesList)
		{
			if(mileStone.Year >= year)
			{
				return mileStone.Clone();
			}
		}
		return null;
	}

	public static bool IsLast(int year)
	{
		EnsureLoaded();
		return _mileStonesList.All(mileStoneData => mileStoneData.Year <= year);
	}

	public static void EnsureLoaded()
	{
		if (_mileStonesList != null)
			return;
		_mileStonesList = new List<MileStoneData>();
		if (!FileAccess.FileExists(JsonPath))
		{
			GD.PrintErr($"找不到建筑配置文件: {JsonPath}");
			return;
		}

		using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
		string jsonString = file.GetAsText();
		_mileStonesList = JsonSerializer.Deserialize<List<MileStoneData>>(jsonString)
		                  ?? new List<MileStoneData>();
	}
}

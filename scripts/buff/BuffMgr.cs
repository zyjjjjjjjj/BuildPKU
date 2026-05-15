using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuffData
{
    public string Id { get; set; }
    public string Description { get; set; }
    public int DurationYears { get; set; }

    public string BuffType { get; set; }
    public List<string> Targets { get; set; }
    public float Multiplier { get; set; }
    public float AddValue { get; set; }
    public string DestBuildingId { get; set; }
    public string SrcBuildingId { get; set; }
}

public class ActiveBuff
{
    public BuffData Data { get; set; }
    public int StartYear { get; set; }

    public bool IsPermanent => Data.DurationYears < 0;

    public bool IsExpired(int currentYear)
    {
        return !IsPermanent && currentYear >= StartYear + Data.DurationYears;
    }

    public int GetRemainingYears(int currentYear)
    {
        if (IsPermanent)
            return -1;

        return Math.Max(0, StartYear + Data.DurationYears - currentYear);
    }
}

public class ScoreContext
{
    public float ResWeight = 1f;
    public float EduWeight = 1f;
    public float EnvWeight = 1f;
}

public partial class BuffMgr : Node
{
    public static BuffMgr Instance { get; private set; }
    
    public IReadOnlyList<ActiveBuff> ActiveWeightBuffs => _activeWeightBuffs;
    private readonly List<ActiveBuff> _activeWeightBuffs = [];
    
    public IReadOnlyDictionary<string, List<ActiveBuff>> ActiveAdjBuffs => _activeAdjBuffs;
    private readonly Dictionary<string, List<ActiveBuff>> _activeAdjBuffs = new Dictionary<string, List<ActiveBuff>>();

    [Signal]
    public delegate void BuffsChangedEventHandler();
    [Signal]
    public delegate void BuffsDecayEventHandler();
    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        TimeMgr.Instance.YearAdvanced += OnYearAdvanced;
    }

    public void AddBuff(BuffData data)
    {
        if (data == null)
            return;

        var newBuff = new ActiveBuff
        {
            Data = data,
            StartYear = TimeMgr.Instance.CurrentYear
        };
        switch (newBuff.Data.BuffType)
        {
            case "ScoreWeightMultiplier":
                _activeWeightBuffs.Add(newBuff);
                break;
            case "AdjBonus":
                if(_activeAdjBuffs.TryGetValue(newBuff.Data.DestBuildingId, out var buffList))
                    buffList.Add(newBuff);
                else
                    _activeAdjBuffs[newBuff.Data.DestBuildingId] = [newBuff];
                break;
        }
        //only apply adj buff immediately
        if (newBuff.Data.BuffType == "AdjBonus")
        {
            var id = newBuff.Data.DestBuildingId;
            BuildingMgr.Instance.SpawnedBuildings.TryGetValue(id, out var buildings);
            if (buildings == null)
                return;
            foreach (var building in buildings)
            {
                building.ApplyAdjBonus(newBuff);
            }
        }
        EmitSignalBuffsChanged();
    }

    private void OnYearAdvanced(int year)
    {
        var removed = _activeWeightBuffs.RemoveAll(buff => buff.IsExpired(year));
        List<string> influencedBuildingIds = [];
        foreach (var pair in _activeAdjBuffs)
        {
            if(pair.Value.Any(buff => buff.IsExpired(year)))
                influencedBuildingIds.Add(pair.Key);
            removed += pair.Value.RemoveAll(buff => buff.IsExpired(year));
        }

        foreach (var id in influencedBuildingIds)
        {
            BuildingMgr.Instance.SpawnedBuildings.TryGetValue(id, out var buildings);
            if (buildings == null)
                continue;
            foreach (var building in buildings)
            {
                building.RecomputeAdjBonus();
            }
        }
        
        if (removed > 0)
        {
            EmitSignalBuffsChanged();
        }
        EmitSignalBuffsDecay();
    }
    public void ApplyWeightBuffs(ScoreContext context)
    {
        foreach (var buff in _activeWeightBuffs.Where(buff => buff.Data.BuffType == "ScoreWeightMultiplier"))
        {
            ApplyWeightBuff(context, buff);
        }
    }

    public void ApplyWeightBuff(ScoreContext context, ActiveBuff buff)
    {
        if (buff.Data.BuffType != "ScoreWeightMultiplier")
            return;
        foreach (var target in buff.Data.Targets)
        {
            switch (target)
            {
                case "Research":
                    context.ResWeight *= buff.Data.Multiplier;
                    break;
                case "Education":
                    context.EduWeight *= buff.Data.Multiplier;
                    break;
                case "Environment":
                    context.EnvWeight *= buff.Data.Multiplier;
                    break;
            }
        }
    }

    public List<ActiveBuff> GetAdjBuffsById(string id)
    {
        _activeAdjBuffs.TryGetValue(id, out var adjBuffs);
        return adjBuffs ?? [];
    }
    
}

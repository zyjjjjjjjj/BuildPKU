using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class BuffData
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string BuffType { get; set; }
    public List<string> Targets { get; set; }
    public string DestBuildingId { get; set; }
    public string SrcBuildingId { get; set; }
}

public class ActiveBuff
{
    public BuffData Data { get; set; }
    public int StartYear { get; set; }
    public int DurationYears { get; set; }
    public int Value { get; set; }

    public string Description
    {
        get
        {
            var desc = Data.Description.Replace("{Value}", Value.ToString());
            return desc + FormattedStatus;
        }
    } 
    
    public string FormattedStatus
    {
        get
        {
            if(IsPermanent)
                return " (永久)";
            var remaining = GetRemainingYears();
            return remaining switch
            {
                > 0 => $" (剩余{remaining}年)",
                < 0 => $" ({Mathf.Abs(remaining)}后生效)",
                _ => " (已失效)"
            };
        }
    }

    public bool IsPermanent => DurationYears < 0;

    public bool IsExpired()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        return !IsPermanent && currentYear >= StartYear + DurationYears;
    }
    
    public bool IsActive()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        return IsPermanent || (currentYear < StartYear + DurationYears && currentYear >= StartYear);
    }

    public int GetRemainingYears()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        if (IsPermanent)
            return -1;
        if (IsExpired())
            return 0;
        if (!IsActive())
            return currentYear - StartYear;
        return Math.Max(0, StartYear + DurationYears - currentYear);
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

    public void AddBuff(ActiveBuff newBuff)
    {
        if (newBuff == null)
            return;
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
            case "InstantMoney":
                MoneyMgr.Instance?.AddMoney(newBuff.Value);
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
        var removed = _activeWeightBuffs.RemoveAll(buff => buff.IsExpired());
        List<string> influencedBuildingIds = [];
        foreach (var pair in _activeAdjBuffs)
        {
            if(pair.Value.Any(buff => buff.IsExpired()))
                influencedBuildingIds.Add(pair.Key);
            removed += pair.Value.RemoveAll(buff => buff.IsExpired());
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
    public void ApplyAllWeightBuffs(ScoreContext context)
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
        float multiplier = (100f + buff.Value) / 100f;
        foreach (var target in buff.Data.Targets)
        {
            switch (target)
            {
                case "Research":
                    context.ResWeight *= multiplier;
                    break;
                case "Education":
                    context.EduWeight *= multiplier;
                    break;
                case "Environment":
                    context.EnvWeight *= multiplier;
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

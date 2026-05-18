using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class ActiveBuff
{
    public BuffData Data;
    public BuffArgs Args;

    public string Description
    {
        get
        {
            var desc = Data.Description;
            if(Args.Value!=0)
                desc = desc.Replace("{Value}", Args.Value.ToString());
            if (Args.Target != String.Empty)
            {
                string displayTarget = Args.Target switch
                {
                    "Research" => "科研",
                    "Education" => "教育",
                    "Environment" => "环境",
                    "Benefit" => "收益",
                    "Population" => "人口",
                    _ => Args.Target
                };
                desc = desc.Replace("{Target}", displayTarget);
            }
            if(Args.SrcBuildingIdOrTag != String.Empty)
            {
                string displaySrc = BuildingDatabase.GetById(Args.SrcBuildingIdOrTag)?.Name ?? Args.SrcBuildingIdOrTag;
                desc = desc.Replace("{Src}", displaySrc);
            }
            if(Args.DestBuildingIdOrTag != String.Empty)            
            {
                string displayDest = BuildingDatabase.GetById(Args.DestBuildingIdOrTag)?.Name ?? Args.DestBuildingIdOrTag;
                desc = desc.Replace("{Dest}", displayDest);
            }
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

    public bool IsPermanent => Args.DurationYears < 0;

    public bool IsExpired()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        return !IsPermanent && currentYear >= Args.StartYear + Args.DurationYears;
    }
    
    public bool IsActive()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        return IsPermanent || (currentYear < Args.StartYear + Args.DurationYears && currentYear >= Args.StartYear);
    }

    public int GetRemainingYears()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        if (IsPermanent)
            return -1;
        if (IsExpired())
            return 0;
        if (!IsActive())
            return currentYear - Args.StartYear;
        return Math.Max(0, Args.StartYear + Args.DurationYears - currentYear);
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
        switch (newBuff.Data.Id)
        {
            case "weight_multiplier_buff":
                _activeWeightBuffs.Add(newBuff);
                break;
            case "adj_bonus_buff":
                if(_activeAdjBuffs.TryGetValue(newBuff.Args.DestBuildingIdOrTag, out var buffList))
                    buffList.Add(newBuff);
                else
                    _activeAdjBuffs[newBuff.Args.DestBuildingIdOrTag] = [newBuff];
                var idOrTag = newBuff.Args.DestBuildingIdOrTag;
                var buildings = BuildingMgr.Instance.GetBuildingsByIdOrTag(idOrTag);
                if (buildings != null)
                {
                    foreach (var building in buildings)
                    {
                        building.ApplyAdjBonus(newBuff);
                    }
                }
                break;
            case "instant_money_buff":
                MoneyMgr.Instance?.AddMoney(newBuff.Args.Value);
                break;
            case "unlock_building_buff":
                BuildingMgr.Instance.UnlockBuildings(newBuff.Args.DestBuildingIdOrTag);
                break;
            case "unlock_region_buff":
                GridMgr.Instance.UnlockRegion(newBuff.Args.DestBuildingIdOrTag);
                break;
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
                building.RecomputeProps();
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
        foreach (var buff in _activeWeightBuffs.Where(buff => buff.Data.Id == "weight_multiplier_buff"))
        {
            ApplyWeightBuff(context, buff);
        }
    }

    public void ApplyWeightBuff(ScoreContext context, ActiveBuff buff)
    {
        if (buff.Data.Id != "weight_multiplier_buff")
            return;
        float multiplier = (100f + buff.Args.Value) / 100f;
        switch (buff.Args.Target)
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

    public List<ActiveBuff> GetAdjBuffs(string id, List<string> Tags)
    {
        var result = new List<ActiveBuff>();
        if(_activeAdjBuffs.TryGetValue(id, out var idBuffs))
            result.AddRange(idBuffs);
        if(Tags == null)
            return result;
        foreach (var tag in Tags)
        {
            if(_activeAdjBuffs.TryGetValue(tag, out var tagBuffs))
                result.AddRange(tagBuffs);
        }
        return result;
    }
    
}

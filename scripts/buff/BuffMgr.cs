using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class ActiveBuff
{
    public BuffData Data;
    public BuffArgs Args;
    public int AcquiredYear;
    public int StartYear => Args.StartYearOffset + AcquiredYear;

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
                string displaySrc = BuildingDatabase.GetNameById(Args.SrcBuildingIdOrTag) ?? Args.SrcBuildingIdOrTag;
                desc = desc.Replace("{Src}", displaySrc);
            }
            if(Args.DestBuildingIdOrTag != String.Empty)            
            {
                string displayDest = BuildingDatabase.GetNameById(Args.DestBuildingIdOrTag) ?? Args.DestBuildingIdOrTag;
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
            if(Args.DurationYears == 0)
                return $" (立即生效)";
            var remaining = GetRemainingYears();
            return remaining switch
            {
                > 0 => $" (剩余{remaining}年)",
                < 0 => $" ({Mathf.Abs(remaining)}年后生效)",
                _ => " (已失效)"
            };
        }
    }

    public bool IsPermanent => Args.DurationYears < 0;

    public bool IsExpired()
    {
        if(IsPermanent)
            return false;
        var currentYear = TimeMgr.Instance.CurrentYear;
        if(Args.DurationYears == 0)
            return currentYear > StartYear;
        return currentYear >= StartYear + Args.DurationYears;
    }
    
    public bool IsActive()
    {
        if (IsPermanent)
            return true;
        var currentYear = TimeMgr.Instance.CurrentYear;
        if(Args.DurationYears == 0)
            return currentYear == StartYear;
        return currentYear >= StartYear && currentYear < StartYear + Args.DurationYears;
    }
    
    public bool StartThisYear()
    {
        return TimeMgr.Instance.CurrentYear == StartYear;
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
        return Math.Max(0, StartYear + Args.DurationYears - currentYear);
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
    
    public List<ActiveBuff> AllBuffs
    {
        get
        {
            var allBuffs = new List<ActiveBuff>();
            allBuffs.AddRange(_weightBuffs);
            foreach (var pair in _adjBuffs)
            {
                allBuffs.AddRange(pair.Value);
            }
            foreach (var pair in _propBuffs)
            {
                allBuffs.AddRange(pair.Value);
            }
            return allBuffs;
        }
    }
    public IReadOnlyList<ActiveBuff> WeightBuffs => _weightBuffs;
    private readonly List<ActiveBuff> _weightBuffs = [];
    public IReadOnlyDictionary<string, List<ActiveBuff>> AdjBuffs => _adjBuffs;
    private readonly Dictionary<string, List<ActiveBuff>> _adjBuffs = new Dictionary<string, List<ActiveBuff>>();
    public IReadOnlyDictionary<string, List<ActiveBuff>> PropBuffs => _propBuffs;
    private readonly Dictionary<string, List<ActiveBuff>> _propBuffs = new Dictionary<string, List<ActiveBuff>>();
    public IReadOnlyList<ActiveBuff> InstantBuffs => _instantBuffs;
    private readonly List<ActiveBuff> _instantBuffs = [];

    [Signal] public delegate void BuffsChangedEventHandler();
    
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
                _weightBuffs.Add(newBuff);
                break;
            case "adj_bonus_buff":
                if(_adjBuffs.TryGetValue(newBuff.Args.DestBuildingIdOrTag, out var buffList))
                    buffList.Add(newBuff);
                else
                    _adjBuffs[newBuff.Args.DestBuildingIdOrTag] = [newBuff];
                break;
            case "instant_money_buff":
            case "instant_turn_based_money_buff":
            case "unlock_building_buff":
            case "unlock_region_buff":
                _instantBuffs.Add(newBuff);
                break;
        }
        if(newBuff.IsActive())
            ApplyBuff(newBuff);
        EmitSignalBuffsChanged();
    }
    
    private void ApplyBuff(ActiveBuff buff)
    {
        switch (buff.Data.Id)
        {
            case "basic_prop_buff":
            case "adj_bonus_buff":
                var idOrTag = buff.Args.DestBuildingIdOrTag;
                var buildings = BuildingMgr.Instance.GetBuildingsByIdOrTag(idOrTag);
                if (buildings != null)
                    foreach (var building in buildings)
                        building.ApplyBuff(buff);
                break;
            case "weight_multiplier_buff":
                break;
            case "instant_money_buff":
                MoneyMgr.Instance.AddMoney(buff.Args.Value);
                break;
            case "instant_turn_based_money_buff":
                MoneyMgr.Instance.AddMoney((TimeMgr.Instance.CurrentYear - TimeMgr.Instance.InitialYear) * buff.Args.Value);
                break;
            case "unlock_building_buff":
                BuildingMgr.Instance.UnlockBuildings(buff.Args.DestBuildingIdOrTag);
                break;
            case "unlock_region_buff":
                GridMgr.Instance.UnlockRegion(buff.Args.DestBuildingIdOrTag);
                break;
        }
    }

    private void OnYearAdvanced(int year)
    {
        _weightBuffs.RemoveAll(buff => buff.IsExpired());
        
        HashSet<string> influencedBuildingIdsOrTags = [];
        foreach (var pair in _adjBuffs)
        {
            if(pair.Value.Any(buff => buff.IsExpired() || buff.StartThisYear()))
                influencedBuildingIdsOrTags.Add(pair.Key);
            pair.Value.RemoveAll(buff => buff.IsExpired());
        }
        foreach (var pair in _propBuffs)
        {
            if(pair.Value.Any(buff => buff.IsExpired() || buff.StartThisYear()))
                influencedBuildingIdsOrTags.Add(pair.Key);
            pair.Value.RemoveAll(buff => buff.IsExpired());
        }
        foreach (var id in influencedBuildingIdsOrTags)
        {
            var buildings = BuildingMgr.Instance.GetBuildingsByIdOrTag(id);
            if (buildings == null)
                continue;
            foreach (var building in buildings)
            {
                building.RecomputeProps();
            }
        }

        foreach (var instantBuff in _instantBuffs.Where(instantBuff => instantBuff.StartThisYear()))
            ApplyBuff(instantBuff);
        _instantBuffs.RemoveAll(buff => buff.IsExpired());
        
        EmitSignalBuffsChanged();
    }
    public void ApplyAllWeightBuffs(ScoreContext context)
    {
        foreach (var buff in _weightBuffs.Where(buff => buff.IsActive()))
            ApplyWeightBuff(context, buff);
    }

    public void ApplyWeightBuff(ScoreContext context, ActiveBuff buff)
    {
        if (buff.Data.Id != "weight_multiplier_buff")
            return;
        float multiplier = ((float)buff.Args.Value) / 100f;
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
        if(_adjBuffs.TryGetValue(id, out var idBuffs))
            result.AddRange(idBuffs);
        if(Tags == null)
            return result;
        foreach (var tag in Tags)
        {
            if(_adjBuffs.TryGetValue(tag, out var tagBuffs))
                result.AddRange(tagBuffs);
        }
        return result;
    }
    
}

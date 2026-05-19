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
            if(desc.Contains("{Value}"))
                desc = desc.Replace("{Value}", FormatFloat(Args.Value));
            if (desc.Contains("ConditionStat"))
                desc = desc.Replace("{ConditionStat}", FormatConditionStat(Args.ConditionStat));
            if (desc.Contains("ConditionOp"))
                desc = desc.Replace("{ConditionOp}", Args.ConditionOp);
            if(desc.Contains("ConditionValue"))
                desc = desc.Replace("{ConditionValue}", FormatFloat(Args.ConditionValue));
            if (desc.Contains("{Target}"))
            {
                var displayTargets = Args.Targets.Select(target => target switch
                    {
                        "Research" => "科研",
                        "Education" => "教育",
                        "Environment" => "环境",
                        "Benefit" => "收益",
                        "Population" => "人口",
                        _ => target
                    })
                    .ToList();
                var displayTarget = string.Join("、", displayTargets);
                desc = desc.Replace("{Target}", displayTarget);
            }
            if(desc.Contains("{Src}"))
            {
                string displaySrc = BuildingDatabase.GetNameById(Args.SrcBuildingIdOrTag) ?? Args.SrcBuildingIdOrTag;
                desc = desc.Replace("{Src}", displaySrc);
            }
            if(desc.Contains("{Dest}"))            
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

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatConditionStat(string stat)
    {
        return stat switch
        {
            "Population" => "人口",
            "Research" => "科研",
            "Education" => "教育",
            "Environment" => "环境",
            "Reputation" => "声誉",
            _ => stat
        };
    }

    private static string FormatTargets(List<string> targets)
    {
        return string.Join("与", targets.Select(FormatTarget));
    }

    private static string FormatTarget(string target)
    {
        return target switch
        {
            "Research" => "科研",
            "Education" => "教育",
            "Environment" => "环境",
            "Benefit" => "收益",
            "Population" => "人口",
            _ => target
        };
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
            case "conditional_weight_add_buff":
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
        switch (buff.Data.Id)
        {
            case "conditional_weight_add_buff":
                if (!CheckCondition(buff))
                    return;
                foreach (var target in buff.Args.Targets)
                    ApplyWeightMultiplier(context, target, ((float)buff.Args.Value) / 100f);
                return;
            case "weight_multiplier_buff":
                float multiplier = buff.Args.Value / 100f;
                foreach (var target in buff.Args.Targets)
                    ApplyWeightMultiplier(context, target, multiplier);
                return;
        }
    }

    private static void ApplyWeightMultiplier(ScoreContext context, string target, float value)
    {
        switch (target)
        {
            case "Research":
                context.ResWeight *= value;
                break;
            case "Education":
                context.EduWeight *= value;
                break;
            case "Environment":
                context.EnvWeight *= value;
                break;
        }
    }

    private static bool CheckCondition(ActiveBuff buff)
    {
        if (string.IsNullOrEmpty(buff.Args.ConditionStat) || string.IsNullOrEmpty(buff.Args.ConditionOp))
            return true;

        float actual = buff.Args.ConditionStat switch
        {
            "Population" => ScoreMgr.Instance.Population,
            "Research" => ScoreMgr.Instance.Research,
            "Education" => ScoreMgr.Instance.Education,
            "Environment" => ScoreMgr.Instance.Environment,
            "Reputation" => ScoreMgr.Instance.Reputation,
            _ => 0f
        };

        return buff.Args.ConditionOp switch
        {
            "<" => actual < buff.Args.ConditionValue,
            "<=" => actual <= buff.Args.ConditionValue,
            ">" => actual > buff.Args.ConditionValue,
            ">=" => actual >= buff.Args.ConditionValue,
            "==" => Mathf.IsEqualApprox(actual, buff.Args.ConditionValue),
            "!=" => !Mathf.IsEqualApprox(actual, buff.Args.ConditionValue),
            _ => false
        };
    }

    public List<ActiveBuff> GetAdjBuffs(string id, List<string> tags)
    {
        var result = new List<ActiveBuff>();
        if(_adjBuffs.TryGetValue(id, out var idBuffs))
            result.AddRange(idBuffs);
        if(tags == null)
            return result;
        foreach (var tag in tags)
        {
            if(_adjBuffs.TryGetValue(tag, out var tagBuffs))
                result.AddRange(tagBuffs);
        }
        return result;
    }
    
}

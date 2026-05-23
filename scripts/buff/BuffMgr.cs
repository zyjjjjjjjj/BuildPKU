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
    public int UsedCnt = 0;
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
                desc = desc.Replace("{Target}", FormatTargets(Args.Targets));
            if(desc.Contains("{Src}"))
                desc = desc.Replace("{Src}", FormatBuildingName(Args.SrcBuildingIdOrTag));
            if(desc.Contains("{Dest}"))            
                desc = desc.Replace("{Dest}", FormatBuildingName(Args.DestBuildingIdOrTag));
            if(desc.Contains("{Limit}"))
                desc = desc.Replace("{Limit}", Args.Limit.ToString());
            return desc + FormattedStatus;
        }
    } 
    
    public string FormattedStatus
    {
        get
        {
            string result = "";
            if(IsPermanent)
                result = " (永久)";
            else if(Args.DurationYears == 0)
                result = " (立即生效)";
            else
            {
                var remaining = GetRemainingYears();
                result = remaining switch
                {
                    > 0 => $" (剩余{remaining}年)",
                    < 0 => $" ({Mathf.Abs(remaining)}年后生效)",
                    _ => " (已失效)"
                };
            }
            if(Args.Limit < 0)
                return result;
            var remainCnt = Math.Max(0, Args.Limit - UsedCnt);
            result += $" (剩余{remainCnt}次)";
            return result;
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

    public bool CheckCondition()
    {
        if(Data.Id != "conditional_weight_add_buff")
            GD.PrintErr("CheckCondition called on non-conditional buff: " + Data.Id);
        if (string.IsNullOrEmpty(Args.ConditionStat) || string.IsNullOrEmpty(Args.ConditionOp))
            return true;

        var scoreCtx = ScoreMgr.Instance.ScoreContext;
        float actual = Args.ConditionStat switch
        {
            "Population" => scoreCtx.Population,
            "Research" => scoreCtx.Research,
            "Education" => scoreCtx.Education,
            "Environment" => scoreCtx.Environment,
            "Reputation" => scoreCtx.Reputation,
            _ => 0f
        };

        return Args.ConditionOp switch
        {
            "<" => actual < Args.ConditionValue,
            "<=" => actual <= Args.ConditionValue,
            ">" => actual > Args.ConditionValue,
            ">=" => actual >= Args.ConditionValue,
            "==" => Mathf.IsEqualApprox(actual, Args.ConditionValue),
            "!=" => !Mathf.IsEqualApprox(actual, Args.ConditionValue),
            _ => false
        };
    }
    public bool CheckRemainCnt()
    {
        if(Data.Id != "cost_reduction_buff_limited")
            GD.PrintErr("CheckRemainCnt called on non-limited buff: " + Data.Id);
        if (Args.Limit < 0)
            return true;
        return Args.Limit > UsedCnt;
    }
    public bool StartThisYear()
    {
        return TimeMgr.Instance.CurrentYear == StartYear;
    }

    public int GetRemainingYears()
    {
        var currentYear = TimeMgr.Instance.CurrentYear;
        if (IsPermanent)
            return 9999;
        if (IsExpired())
            return 0;
        if (!IsActive())
            return currentYear - StartYear;
        return Math.Max(0, StartYear + Args.DurationYears - currentYear);
    }
    
    private static string FormatFloat(float value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }
    private static string FormatConditionStat(string stat)
    {
        return stat switch
        {
            "Population" => "总人口",
            "Research" => "总科研评分",
            "Education" => "总教学评分",
            "Environment" => "总环境评分",
            "Reputation" => "全球声誉",
            _ => stat
        };
    }
    private static string FormatTargets(List<string> targets)
    {
        var displayTargets = targets.Select(target => target switch
        {
            "Research" => "科研",
            "Education" => "教学",
            "Environment" => "环境",
            "Benefit" => "收益",
            "Population" => "人口",
            _ => target
        }).ToList();
        return string.Join("、", displayTargets);
    }
    private static string FormatBuildingName(string idOrTag)
    {
        return BuildingDatabase.GetNameById(idOrTag) ?? idOrTag;
    }
}

public partial class BuffMgr : Node
{
    public static BuffMgr Instance { get; private set; }
    private readonly List<ActiveBuff> _weightBuffs = [];
    public IReadOnlyDictionary<string, List<ActiveBuff>> BuildingBuffs => _buildingBuffs;
    private readonly Dictionary<string, List<ActiveBuff>> _buildingBuffs = new Dictionary<string, List<ActiveBuff>>();
    
    private readonly List<ActiveBuff> _instantBuffs = [];
    public IReadOnlyList<ActiveBuff> TuitionBuffs => _tuitionBuffs;
    private readonly List<ActiveBuff> _tuitionBuffs = [];
    private readonly Dictionary<string, List<ActiveBuff>> _costBuffs = new Dictionary<string, List<ActiveBuff>>();
    private Vector3 _accDeltaWeight;//add_min_weight_per_turn_buff专用
    private readonly List<ActiveBuff> _perTurnBuffs = new List<ActiveBuff>();//每回合都要触发的buff
    private readonly List<ActiveBuff> _scoreBuffs = new List<ActiveBuff>();
    
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
        BuildingMgr.Instance.SpawnedBuilding += IncreaseUsedCnt;
    }

    public void AddBuff(ActiveBuff newBuff)
    {
        if (newBuff == null)
            return;
        switch (newBuff.Data.Id)
        {
            case "weight_add_buff":
            case "conditional_weight_add_buff":
                _weightBuffs.Add(newBuff);
                break;
            case "adj_bonus_with_cnt_buff":
            case "adj_bonus_with_tolerance_buff":
            case "adj_bonus_same_id_buff":
            case "basic_prop_buff":
            case "adj_bonus_buff":
                AddBuffToDict(_buildingBuffs, newBuff);
                break;
            case "tuition_buff":
                _tuitionBuffs.Add(newBuff);
                break;
            case "instant_money_buff":
            case "instant_turn_based_money_buff":
            case "unlock_building_buff":
            case "unlock_region_buff":
            case "unlock_upgrading_buff":
                _instantBuffs.Add(newBuff);
                break;
            case "cost_upper_bound_buff":
            case "cost_reduction_buff":
            case "cost_reduction_buff_limited":
                AddBuffToDict(_costBuffs, newBuff);
                break;
            case "add_min_weight_per_turn_buff":
                _perTurnBuffs.Add(newBuff);
                break;
            case "score_transform_buff":
                _scoreBuffs.Add(newBuff);
                break;
            default:
                GD.PrintErr("Unknown buff id: " + newBuff.Data.Id);
                return;
        }
        if(newBuff.IsActive())
            ApplyBuff(newBuff);
    }
    
    private void ApplyBuff(ActiveBuff buff)
    {
        switch (buff.Data.Id)
        {
            case "adj_bonus_with_cnt_buff":
            case "adj_bonus_with_tolerance_buff":
            case "adj_bonus_same_id_buff":
            case "basic_prop_buff":
            case "adj_bonus_buff":
                var idOrTag = buff.Args.DestBuildingIdOrTag;
                var buildings = BuildingMgr.Instance.GetBuildingsByIdOrTag(idOrTag);
                if (buildings != null)
                    foreach (var building in buildings)
                        building.ApplyBuff(buff);
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
            case "unlock_upgrading_buff":
                BuildingMgr.Instance.UnlockUpgradedBuildings(buff.Args.DestBuildingIdOrTag);
                break;
            case "unlock_region_buff":
                GridMgr.Instance.UnlockRegion(buff.Args.DestBuildingIdOrTag);
                break;
            case "cost_upper_bound_buff":
            case "cost_reduction_buff":            
            case "cost_reduction_buff_limited":
                BuildingSideBar.Instance.RefreshItemList();
                break;
            case "add_min_weight_per_turn_buff":
                var weights = ScoreMgr.Instance.Weights;
                if (weights.EnvWeight <= weights.ResWeight && weights.EnvWeight <= weights.EduWeight)
                    _accDeltaWeight.Z += buff.Args.Value;
                else if(weights.EduWeight <= weights.ResWeight && weights.EduWeight <= weights.EnvWeight)
                    _accDeltaWeight.Y += buff.Args.Value;
                else
                    _accDeltaWeight.X += buff.Args.Value;
                break;
        }
    }

    private void OnYearAdvanced(int year)
    {
        _weightBuffs.RemoveAll(buff => buff.IsExpired());
        _tuitionBuffs.RemoveAll(buff => buff.IsExpired());
        _scoreBuffs.RemoveAll(buff => buff.IsExpired());
        _costBuffs.Values.ToList().ForEach(buffList => buffList.RemoveAll(buff => buff.IsExpired()));
        
        HashSet<string> influencedBuildingIdsOrTags = [];
        foreach (var pair in _buildingBuffs)
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
        foreach (var perTurnBuff in _perTurnBuffs.Where(perTurnBuff => perTurnBuff.IsActive()))
            ApplyBuff(perTurnBuff);
        _perTurnBuffs.RemoveAll(buff => buff.IsExpired());
    }
    public void ApplyAllWeightBuffs(Weights weight)
    {
        foreach (var buff in _weightBuffs.Where(buff => buff.IsActive()))
        {
            ApplyWeightBuff(weight, buff);
        }
        weight.ResWeight += _accDeltaWeight.X;
        weight.EduWeight += _accDeltaWeight.Y;
        weight.EnvWeight += _accDeltaWeight.Z;
        return;
    }

    public void ApplyWeightBuff(Weights weight, ActiveBuff buff)
    {
        if (buff.Data.Id == "conditional_weight_add_buff" && !buff.CheckCondition())
            return;
        foreach (var target in buff.Args.Targets)
            AddWeight(weight, target, buff.Args.Value);
    }

    private void AddWeight(Weights weight, string target, float value)
    {
        switch (target)
        {
            case "Research":
                weight.ResWeight += value;
                break;
            case "Education":
                weight.EduWeight += value;
                break;
            case "Environment":
                weight.EnvWeight += value;
                break;
        }
    }
    
    public List<ActiveBuff> GetRelatedBuffs(string id, List<string> tags)
    {
        var result = new List<ActiveBuff>();
        if(_buildingBuffs.TryGetValue(id, out var idBuffs))
            result.AddRange(idBuffs);
        if(tags == null)  
            return result;
        foreach (var tag in tags)
        {
            if (_buildingBuffs.TryGetValue(tag, out var tagBuffs))
                result.AddRange(tagBuffs);
        }
        return result;
    }
    
    public List<ActiveBuff> GetCostBuffs(string id, List<string> tags)
    {
        var result = new List<ActiveBuff>();
        if(_costBuffs.TryGetValue(id, out var idBuffs))
            result.AddRange(idBuffs);
        if(tags == null)
            return result;
        foreach (var tag in tags)
        {
            if(_costBuffs.TryGetValue(tag, out var tagBuffs))
                result.AddRange(tagBuffs);
        }
        return result;
    }

    public List<ActiveBuff> GetScoreBuffs()
    {
        return _scoreBuffs;
    }
    
    private void AddBuffToDict(Dictionary<string,List<ActiveBuff>> dict, ActiveBuff buff)
    {
        if(dict.TryGetValue(buff.Args.DestBuildingIdOrTag, out var buffList))
            buffList.Add(buff);
        else
            dict[buff.Args.DestBuildingIdOrTag] = [buff];
    }
    
    private void IncreaseUsedCnt(BuildingBase building)
    {
        if(_costBuffs.TryGetValue(building.Id, out var idBuffs))
            foreach (var buff in idBuffs)
                buff.UsedCnt++;
        if(building.Tags == null)
            return;
        foreach (var tag in building.Tags)
        {
            if (!_costBuffs.TryGetValue(tag, out var tagBuffs)) continue;
            foreach (var buff in tagBuffs)
                buff.UsedCnt++;
        }
        BuffSideBar.Instance.RefreshCardList();
        BuildingSideBar.Instance.RefreshItemList();
    }
}

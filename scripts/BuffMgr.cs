using Godot;
using System;
using System.Collections.Generic;

public class BuffData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int DurationYears { get; set; }
    public List<BuffEffectData> Effects { get; set; }
}

public class BuffEffectData
{
    public string EffectType { get; set; }
    public string Target { get; set; }
    public float Multiplier { get; set; }
    public float AddValue { get; set; }
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

    private readonly List<ActiveBuff> _activeBuffs = [];

    public IReadOnlyList<ActiveBuff> ActiveBuffs => _activeBuffs;

    [Signal]
    public delegate void BuffsChangedEventHandler();

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

        _activeBuffs.Add(new ActiveBuff
        {
            Data = data,
            StartYear = TimeMgr.Instance.CurrentYear
        });

        EmitSignalBuffsChanged();
        ScoreMgr.Instance.RecomputeScoreWeights();
    }

    private void OnYearAdvanced(int year)
    {
        int removed = _activeBuffs.RemoveAll(buff => buff.IsExpired(year));

        EmitSignalBuffsChanged();

        if (removed > 0)
            ScoreMgr.Instance.RecomputeScoreWeights();
    }
    
    public void ApplyBuffs(ScoreContext context)
    {
        foreach (var buff in _activeBuffs)
        {
            foreach (var effect in buff.Data.Effects)
            {
                switch (effect.EffectType)
                {
                    case "ScoreWeightMultiplier":
                        ApplyScoreWeightMultiplier(context, effect);
                        break;
                }
            }
        }
    }

    private void ApplyScoreWeightMultiplier(ScoreContext context, BuffEffectData effect)
    {
        switch (effect.Target)
        {
            case "Research":
                context.ResWeight *= effect.Multiplier;
                break;
            case "Education":
                context.EduWeight *= effect.Multiplier;
                break;
            case "Environment":
                context.EnvWeight *= effect.Multiplier;
                break;
        }
    }
    
}

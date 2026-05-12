using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BuffSideBar : PanelContainer
{
    public static BuffSideBar Instance { get; private set; }
    private readonly List<ActiveCardView> _activeCards = [];
    
    [Export] private VBoxContainer _buffList;

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
        BuffMgr.Instance.BuffsDecay += Refresh;
        BuffMgr.Instance.BuffsChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        foreach (var child in _buffList.GetChildren())
            child.QueueFree();

        _activeCards.RemoveAll(card => card.IsExpired(TimeMgr.Instance.CurrentYear));

        foreach (var activeCard in _activeCards)
        {
            var label = new Label();

            int remainingYears = activeCard.GetRemainingYears(TimeMgr.Instance.CurrentYear);
            string duration = remainingYears < 0 ? "永久" : $"剩余 {remainingYears} 年";

            label.Text = $"{activeCard.Card.Name} ({duration})";
            label.TooltipText = BuildTooltip(activeCard);
            label.MouseFilter = Control.MouseFilterEnum.Stop;
            
            _buffList.AddChild(label);
        }
    }
    
    private string BuildTooltip(ActiveCardView activeCard)
    {
        var lines = new List<string>();

        foreach (var buff in activeCard.Buffs)
        {
            string duration = buff.DurationYears < 0 ? "永久" : $"{buff.DurationYears} 年";
            lines.Add($"{buff.Description} ({duration})");
        }

        return string.Join("\n", lines);
    }
    
    public void AddCard(CardData card, List<BuffData> buffs)
    {
        if (card == null || buffs == null || buffs.Count == 0)
            return;

        _activeCards.Add(new ActiveCardView
        {
            Card = card,
            Buffs = buffs,
            StartYear = TimeMgr.Instance.CurrentYear
        });

        Refresh();
    }
}

public class ActiveCardView
{
    public CardData Card { get; set; }
    public List<BuffData> Buffs { get; set; }
    public int StartYear { get; set; }

    public int DurationYears
    {
        get
        {
            if (Buffs == null || Buffs.Count == 0)
                return 0;

            if (Buffs.Any(buff => buff.DurationYears < 0))
                return -1;

            return Buffs.Max(buff => buff.DurationYears);
        }
    }

    public bool IsPermanent => DurationYears < 0;

    public bool IsExpired(int currentYear)
    {
        return !IsPermanent && currentYear >= StartYear + DurationYears;
    }

    public int GetRemainingYears(int currentYear)
    {
        if (IsPermanent)
            return -1;

        return Math.Max(0, StartYear + DurationYears - currentYear);
    }
}
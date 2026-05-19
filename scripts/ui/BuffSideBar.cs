using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BuffSideBar : PanelContainer
{
    public static BuffSideBar Instance { get; private set; }

    private readonly List<ActiveCard> _collectedCards = [];
    private readonly List<ActiveBuff> _mockAdjBuffs = [];
    private bool _isExpanded = true;

    [Export] private bool _useMockData = false;
    [Export] private int _mockCardCount = 24;
    [Export] private int _mockAdjBonusCount = 16;
    [Export] private Button _toggleButton;
    [Export] private VBoxContainer _cardList;
    [Export] private Control _adjBonusTitleLabel;
    [Export] private VBoxContainer _adjBonusList;
    [Export] private string _cardSectionTitle = "卡片栏";

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
        if (_toggleButton != null)
            _toggleButton.Pressed += ToggleExpanded;
        BuffMgr.Instance.BuffsChanged += Refresh;
        //AddMockDataIfNeeded();
        Refresh();
    }

    private void ToggleExpanded()
    {
        _isExpanded = !_isExpanded;
        Refresh();
    }

    private void Refresh()
    {
        RefreshCardList();
        RefreshAdjBonusList();
    }

    public void RefreshCardList()
    {
        if (_toggleButton != null)
            _toggleButton.Text = _isExpanded ? $"{_cardSectionTitle} <" : $"{_cardSectionTitle} >";

        if (_cardList == null)
            return;

        SetListVisible(_cardList, _isExpanded);

        foreach (var child in _cardList.GetChildren())
            child.QueueFree();

        if (!_isExpanded)
            return;

        if (_collectedCards.Count == 0)
        {
            _cardList.AddChild(CreateMutedLabel("暂无卡牌"));
            return;
        }

        foreach (var activeCard in _collectedCards)
        {
            var label = new Label
            {
                Text = activeCard.Data.Name,
                TooltipText = BuildCardTooltip(activeCard),
                MouseFilter = Control.MouseFilterEnum.Pass,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(160, 0)
            };

            _cardList.AddChild(label);
        }
    }

    private void RefreshAdjBonusList()
    {
        if (_adjBonusTitleLabel != null)
            SetControlText(_adjBonusTitleLabel, "相邻加成效果");

        if (_adjBonusList == null)
            return;

        foreach (var child in _adjBonusList.GetChildren())
            child.QueueFree();

        var adjBuffs = GetActiveAdjBuffs();
        if (adjBuffs.Count == 0)
        {
            _adjBonusList.AddChild(CreateMutedLabel("暂无相邻加成"));
            return;
        }

        foreach (var buff in adjBuffs)
        {
            var label = new Label
            {
                Text = BuildAdjBonusText(buff),
                TooltipText = BuildBuffTooltip(buff),
                MouseFilter = Control.MouseFilterEnum.Pass,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(160, 0)
            };

            _adjBonusList.AddChild(label);
        }
    }

    private List<ActiveBuff> GetActiveAdjBuffs()
    {
        int currentYear = TimeMgr.Instance.CurrentYear;
        return BuffMgr.Instance.AdjBuffs.Values
            .SelectMany(buffs => buffs)
            .Concat(_mockAdjBuffs)
            .Where(buff => buff != null && buff.Data != null)
            .Where(buff => buff.Data.Id == "adj_bonus_buff")
            .Where(buff => !buff.IsExpired())
            .ToList();
    }

    private string BuildCardTooltip(ActiveCard activeCard)
    {
        var lines = new List<string>();

        // if (!string.IsNullOrEmpty(activeCard.Card.Description))
        //     lines.Add(activeCard.Card.Description);

        foreach (var buff in activeCard.Buffs)
            lines.Add(BuildBuffTooltip(buff));

        return string.Join("\n", lines.Where(line => !string.IsNullOrEmpty(line)));
    }

    private string BuildBuffTooltip(ActiveBuff buff)
    {
        if (buff?.Data == null)
            return string.Empty;
        
        //string duration = FormatDuration(buff.DurationYears, buff.StartYear);
        return buff.Description;
    }

    private string BuildAdjBonusText(ActiveBuff buff)
    {
        return buff.Description;
        var data = buff.Data;
        string duration = FormatDuration(buff.Args.DurationYears, buff.StartYear);
        string destBuildingName = GetBuildingDisplayName(buff.Args.DestBuildingIdOrTag);
        string srcBuildingName = GetBuildingDisplayName(buff.Args.SrcBuildingIdOrTag);
        return $"{destBuildingName} <- {srcBuildingName} ({duration})";
    }

    private string GetBuildingDisplayName(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return string.Empty;

        return BuildingDatabase.GetById(buildingId)?.Name ?? buildingId;
    }

    private string FormatDuration(int durationYears, int startYear)
    {
        if (durationYears < 0)
            return "永久";

        if (durationYears == 0)
            return "立即生效";
        
        if(startYear > TimeMgr.Instance.CurrentYear)
            return $"将在 {startYear - TimeMgr.Instance.CurrentYear} 年后生效";

        int remainingYears = Math.Max(0, startYear + durationYears - TimeMgr.Instance.CurrentYear);
        return remainingYears == 0 ? "已过期" : $"剩余 {remainingYears} 年";
    }

    private Label CreateMutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(160, 0),
            Modulate = new Color(1f, 1f, 1f, 0.65f)
        };
    }

    private void SetControlText(Control control, string text)
    {
        switch (control)
        {
            case Label label:
                label.Text = text;
                break;
            case Button button:
                button.Text = text;
                break;
        }
    }

    private void SetListVisible(Control list, bool visible)
    {
        list.Visible = visible;

        if (list.GetParent() is ScrollContainer scrollContainer)
            scrollContainer.Visible = visible;
    }
/*
    private void AddMockDataIfNeeded()
    {
        if (!_useMockData)
            return;

        if (_collectedCards.Count == 0)
            AddMockCards();

        if (_mockAdjBuffs.Count == 0)
            AddMockAdjBuffs();
    }


    private void AddMockCards()
    {
        for (int i = 0; i < _mockCardCount; i++)
        {
            var buffs = new List<BuffData>
            {
                CreateMockWeightBuff(i, "Research", 1.1f + i % 5 * 0.05f),
                CreateMockWeightBuff(i, i % 2 == 0 ? "Education" : "Environment", 1.05f + i % 4 * 0.05f)
            };
            var activeBuffs = buffs.Select(buff => new ActiveBuff
            {
                Data = buff,
                Args.StartYear = TimeMgr.Instance.CurrentYear - i % 5,
                DurationYears = i % 5,
                Value = 20,
            }).ToList();

            _collectedCards.Add(new ActiveCard
            {
                Data = new CardData
                {
                    Id = $"mock_card_{i}",
                    Name = $"\u6d4b\u8bd5\u5361 {i + 1:00}",
                    Description = "\u4ec5\u7528\u4e8e\u9884\u89c8\u5927\u91cf\u5361\u724c\u65f6\u7684 UI \u6548\u679c",
                    CardType = "Buff",
                    Rarity = i % 3 == 0 ? "Epic" : "Common",
                    Repeatable = true,
                    Buffs = []
                },
                Buffs = activeBuffs,
            });
        }
    }

    private void AddMockAdjBuffs()
    {
        string[] destBuildingIds = ["lab", "library", "teaching", "cod_lab", "pc_lab", "dpt_office"];
        string[] srcBuildingIds = ["green", "lake", "villa", "ground", "canteen", "hall"];
        string[] targets = ["Research", "Education", "Environment", "Benefit"];

        for (int i = 0; i < _mockAdjBonusCount; i++)
        {
            _mockAdjBuffs.Add(new ActiveBuff
            {
                Data = new BuffData
                {
                    Id = $"mock_adj_bonus_{i}",
                    Description = $"\u4e0e{GetBuildingDisplayName(srcBuildingIds[i % srcBuildingIds.Length])}\u76f8\u90bb\u65f6\uff0c{FormatTargetName(targets[i % targets.Length])}\u4ea7\u51fa\u63d0\u9ad8 {10 + i % 5 * 5}%",
                },
                DurationYears = i % 4 == 0 ? -1 : 6 + i % 8,
                StartYear = TimeMgr.Instance.CurrentYear - i % 5,
                Value = 10 + i % 5 * 5,
            });
        }
    }

    private BuffData CreateMockWeightBuff(int index, string target, float multiplier)
    {
        return new BuffData
        {
            Id = $"mock_weight_buff_{index}_{target}",
            Description = $"{FormatTargetName(target)}\u6743\u91cd\u63d0\u9ad8 {Mathf.RoundToInt((multiplier - 1f) * 100f)}%",
        };
    }
*/
    private string FormatTargetName(string target)
    {
        return target switch
        {
            "Research" => "\u79d1\u7814",
            "Education" => "\u6559\u5b66",
            "Environment" => "\u73af\u5883",
            "Population" => "\u4eba\u53e3",
            "Benefit" => "\u6536\u76ca",
            _ => target
        };
    }

    public void AddCard(ActiveCard activeCard)
    {
        if (activeCard == null)
            return;
        _collectedCards.Add(activeCard);
        Refresh();
    }
}

using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CardChoicePanel : PanelContainer
{
    public static CardChoicePanel Instance { get; private set; }

    [Signal]
    public delegate void CardChosenEventHandler(string cardId);

    [Export] private Label _titleLabel;
    [Export] private Button[] _cardButtons;
    [Export] private Label[] _cardNameLabels;
    [Export] private Label[] _cardDescriptionLabels;
    [Export] private Label[] _cardDurationLabels;

    private List<CardData> _currentCards = [];

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
        Visible = false;
        EnsureCardLabels();

        for (int i = 0; i < _cardButtons.Length; i++)
        {
            int index = i;
            _cardButtons[i].Pressed += () => ChooseIndex(index);
        }
    }

    public void ShowChoices(List<CardData> cards)
    {
        _currentCards = cards;
        Visible = true;

        _titleLabel.Text = "选择一张发展方针";

        for (int i = 0; i < _cardButtons.Length; i++)
        {
            if (i >= cards.Count)
            {
                _cardButtons[i].Visible = false;
                ClearCardLabels(i);
                continue;
            }

            var card = cards[i];
            _cardButtons[i].Visible = true;
            _cardButtons[i].Text = string.Empty;
            SetCardLabels(i, card);
        }
    }

    public void HideChoices()
    {
        Visible = false;
    }

    private void ChooseIndex(int index)
    {
        if (index < 0 || index >= _currentCards.Count)
            return;

        EmitSignalCardChosen(_currentCards[index].Id);
    }

    private void EnsureCardLabels()
    {
        if (LabelsAreReady())
            return;

        _cardNameLabels = new Label[_cardButtons.Length];
        _cardDescriptionLabels = new Label[_cardButtons.Length];
        _cardDurationLabels = new Label[_cardButtons.Length];

        for (int i = 0; i < _cardButtons.Length; i++)
        {
            _cardNameLabels[i] = _cardButtons[i].GetNodeOrNull<Label>("MarginContainer/VBoxContainer/NameLabel");
            _cardDescriptionLabels[i] = _cardButtons[i].GetNodeOrNull<Label>("MarginContainer/VBoxContainer/DescriptionLabel");
            _cardDurationLabels[i] = _cardButtons[i].GetNodeOrNull<Label>("MarginContainer/VBoxContainer/DurationLabel");
        }
    }

    private bool LabelsAreReady()
    {
        return _cardNameLabels != null
               && _cardDescriptionLabels != null
               && _cardDurationLabels != null
               && _cardNameLabels.Length >= _cardButtons.Length
               && _cardDescriptionLabels.Length >= _cardButtons.Length
               && _cardDurationLabels.Length >= _cardButtons.Length;
    }

    private void SetCardLabels(int index, CardData card)
    {
        if (!HasCardLabels(index))
        {
            _cardButtons[index].Text = FormatCardText(card);
            return;
        }

        _cardNameLabels[index].Text = card.Name;
        _cardDescriptionLabels[index].Text = card.Description;
        _cardDurationLabels[index].Text = FormatCardDuration(card);
    }

    private void ClearCardLabels(int index)
    {
        if (!HasCardLabels(index))
            return;

        _cardNameLabels[index].Text = string.Empty;
        _cardDescriptionLabels[index].Text = string.Empty;
        _cardDurationLabels[index].Text = string.Empty;
    }

    private bool HasCardLabels(int index)
    {
        return index >= 0
               && _cardNameLabels != null
               && _cardDescriptionLabels != null
               && _cardDurationLabels != null
               && index < _cardNameLabels.Length
               && index < _cardDescriptionLabels.Length
               && index < _cardDurationLabels.Length
               && _cardNameLabels[index] != null
               && _cardDescriptionLabels[index] != null
               && _cardDurationLabels[index] != null;
    }

    private string FormatCardText(CardData card)
    {
        return $"{card.Name}\n\n{card.Description}\n\n{FormatCardDuration(card)}";
    }

    private string FormatCardDuration(CardData card)
    {
        if (card.Buffs == null || card.Buffs.Count == 0)
            return "[无持续效果]";

        var buffsProps = card.Buffs.Values.ToList();

        if (buffsProps.Count == 0)
            return "[无持续效果]";

        if (buffsProps.All(buff => buff.DurationYears == 0))
            return "[立即生效]";

        if (buffsProps.Any(buff => buff.DurationYears < 0))
            return "[永久]";

        int maxDuration = buffsProps.Max(buff => buff.DurationYears);
        return $"[持续 {maxDuration} 年]";
    }

}

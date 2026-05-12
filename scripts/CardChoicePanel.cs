using Godot;
using System.Collections.Generic;

public partial class CardChoicePanel : PanelContainer
{
    public static CardChoicePanel Instance { get; private set; }

    [Signal]
    public delegate void CardChosenEventHandler(string cardId);

    [Export] private Label _titleLabel;
    [Export] private Button[] _cardButtons;

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
                continue;
            }

            var card = cards[i];
            _cardButtons[i].Visible = true;
            _cardButtons[i].Text = FormatCardText(card);
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

    private string FormatCardText(CardData card)
    {
        string duration = card.DurationYears < 0 ? "永久" : $"持续 {card.DurationYears} 年";
        return $"{card.Name}\n\n{card.Description}\n\n{duration}";
    }
}
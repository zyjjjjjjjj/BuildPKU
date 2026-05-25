using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ActiveCard
{
    public CardData Data { get; set; }
    public int AcquiredYear { get; set; }
    public List<ActiveBuff> Buffs { get; set; }
}

public partial class CardMgr : Node
{
    public static CardMgr Instance { get; private set; }

    [Export] private int _cardIntervalYears = 5;
    [Export] private int _choiceCount = 3;

    public bool IsChoosingCard { get; private set; }

    private readonly HashSet<string> _pickedNonRepeatableCardIds = [];
    private List<CardData> _currentChoices = [];

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
        CardChoicePanel.Instance.CardChosen += OnCardChosen;
    }

    private void OnYearAdvanced(int year)
    {
        ActivateEvent(year);
        if ((year - TimeMgr.Instance.InitialYear) % _cardIntervalYears != 0)
        {
            GameMgr.Instance.StartNewTurn();
            return;
        }

        ShowCardChoices(year);
    }

    private void ShowCardChoices(int year)
    {
        var candidates = CardDatabase.GetAll()
            .Where(card => card.MinYear <= year)
            .Where(card => card.Repeatable || !_pickedNonRepeatableCardIds.Contains(card.Id))
            .ToList();

        _currentChoices = PickRandomCards(candidates, _choiceCount);

        if (_currentChoices.Count == 0)
        {
            GD.Print($"No available cards in year {year}");
            return;
        }

        IsChoosingCard = true;
        CardChoicePanel.Instance.ShowChoices(_currentChoices);
        AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.CardShow);
    }

    private List<CardData> PickRandomCards(List<CardData> candidates, int count)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        var result = new List<CardData>();

        while (result.Count < count && candidates.Count > 0)
        {
            int index = rng.RandiRange(0, candidates.Count - 1);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return result;
    }

    private void OnCardChosen(string cardId)
    {
        var card = _currentChoices.FirstOrDefault(card => card.Id == cardId);

        if (card == null)
        {
            GD.PrintErr($"Chosen card not found: {cardId}");
            return;
        }

        GD.Print($"Chosen card: {card.Name}");
        AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.CardChoose);

        IsChoosingCard = false;
        _currentChoices.Clear();
        CardChoicePanel.Instance.HideChoices();
        ActivateCard(card);
        GameMgr.Instance.StartNewTurn();
    }

    private void ActivateCard(CardData cardData)
    {
        var activeCard = new ActiveCard
        {
            Data = cardData,
            AcquiredYear = TimeMgr.Instance.CurrentYear
        };
        
        var buffs = new List<ActiveBuff>();

        if (cardData.CardType == "Buff" && cardData.Buffs != null)
        {
            foreach (var pair in cardData.Buffs)
            {
                var id = Regex.Replace(pair.Key, @"_\d+$", "");
                var args = pair.Value;
                var buffData = BuffDatabase.GetById(id);
                if(buffData == null)
                {
                    GD.PrintErr($"Buff data not found for id: {id}");
                    continue;
                }
                var newBuff = new ActiveBuff
                {
                    Data = buffData,
                    Args = args,
                    AcquiredYear = TimeMgr.Instance.CurrentYear
                };

                buffs.Add(newBuff);
                BuffMgr.Instance.AddBuff(newBuff);
            }
        }
        
        activeCard.Buffs = buffs;

        BuffSideBar.Instance.AddCard(activeCard);
        if (!cardData.Repeatable)
            _pickedNonRepeatableCardIds.Add(cardData.Id);
    }

    public void ActivateEvent(int year)
    {
        var evt = CardDatabase.GetEvent(year);
        if(evt == null)
            return;
        foreach (var cardId in evt.CardIds)
        {
            var cardData = CardDatabase.GetCardDataById(cardId);
            if (cardData == null)
                return;
            ActivateCard(cardData);
        }
    }
}

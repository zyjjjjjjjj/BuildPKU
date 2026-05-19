using Godot;
using System;
using System.Linq;

public partial class MoneyMgr : Node
{
	public static MoneyMgr Instance { get; private set; }

	[Export] public float InitialMoney = 10000;
	[Export] public float Initial_Tuition = 1000;

	public float CurrentMoney
	{
		get => _currentMoney;
		private set
		{
			_currentMoney = value;
			EmitSignalMoneyChanged();
		}
	}
	private float _currentMoney;
	
	public float Tuition => CalcCurrentTuition();

	public float MoneyPerYear => ScoreMgr.Instance.ScoreContext.Benefit;

	[Signal]
	public delegate void MoneyChangedEventHandler();

	
	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if(Instance == this)
			Instance = null;
	}
	
	public override void _Ready()
	{
		_currentMoney = InitialMoney;
		TimeMgr.Instance.YearAdvanced += _OnYearAdvanced;
	}

	private void _OnYearAdvanced(int year)
	{
		if(TimeMgr.Instance.InitialYear == year)
			return;
		AddMoney(MoneyPerYear);
	}

	public void AddMoney(float amount)
	{
		CurrentMoney += amount;
	}

	public bool CanAfford(float cost)
	{
		return CurrentMoney >= cost;
	}

	public bool TrySpend(float cost)
	{
		if (!CanAfford(cost))
			return false;

		CurrentMoney -= cost;
		return true;
	}
	
	private float CalcCurrentTuition()
	{
		float tuition = Initial_Tuition;
		foreach (var buff in BuffMgr.Instance.TuitionBuffs.Where(buff=>buff.IsActive()))
		{
			var multiplier = buff.Args.Value / 100f;
			tuition *= multiplier;
		}
		return tuition;
	}
}


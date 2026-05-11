using Godot;
using System;

public partial class MoneyMgr : Node
{
	public static MoneyMgr Instance { get; private set; }

	[Export] public int InitialMoney = 2000;
	[Export] public int MoneyPerYear = 100;

	private int _currentMoney;

	public int CurrentMoney
	{
		get => _currentMoney;
		private set
		{
			_currentMoney = value;
			EmitSignalMoneyChanged(_currentMoney);
		}
	}

	[Signal]
	public delegate void MoneyChangedEventHandler(int newMoney);

	public override void _Ready()
	{
		Instance = this;
		CurrentMoney = InitialMoney;

		if (TimeMgr.Instance != null)
		{
			TimeMgr.Instance.YearAdvanced += _OnYearAdvanced;
		}
	}

	private void _OnYearAdvanced(int year)
	{
		AddMoney(MoneyPerYear);
	}

	public void AddMoney(int amount)
	{
		CurrentMoney += amount;
	}

	public bool CanAfford(int cost)
	{
		return CurrentMoney >= cost;
	}

	public bool TrySpend(int cost)
	{
		if (!CanAfford(cost))
			return false;

		CurrentMoney -= cost;
		return true;
	}
}


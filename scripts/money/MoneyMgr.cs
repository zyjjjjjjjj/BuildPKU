using Godot;
using System;
using System.Linq;

public partial class MoneyMgr : Node
{
	public static MoneyMgr Instance { get; private set; }

	[Export] public float InitialMoney = 10000;
	[Export] public float Tuition = 1000;

	private float _currentMoney;

	public float CurrentMoney
	{
		get => _currentMoney;
		private set
		{
			_currentMoney = value;
			EmitSignalMoneyChanged();
		}
	}

	public float BuildingBenefits => _buildingBenefits;
	private float _buildingBenefits = 0;

	public float MoneyPerYear => Tuition * ScoreMgr.Instance.Population + _buildingBenefits;

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
		BuffMgr.Instance.BuffsChanged += RecomputeBenefits;
		BuildingMgr.Instance.SpawnedBuilding += _ => RecomputeBenefits();
		BuildingMgr.Instance.DestroyedBuilding += RecomputeBenefits;
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
	
	public void RecomputeBenefits()
	{
		_buildingBenefits = 0;
		var spawnedBuildings = BuildingMgr.Instance.SpawnedBuildings;
		foreach (var building in spawnedBuildings.SelectMany(pair => pair.Value))
		{
			_buildingBenefits += building.Benefit;
		}
		EmitSignalMoneyChanged();
	}
}


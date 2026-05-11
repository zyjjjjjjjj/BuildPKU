using Godot;
using System;

public partial class TopBar : HBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	private Label _yearLabel;
	private Button _nextYearButton;
	private Label _moneyLabel;
	
	public override void _Ready()
	{		
		_yearLabel = GetNode<Label>("YearLabel");
		TimeMgr.Instance.YearAdvanced += _yearUpdate;
		_yearUpdate(TimeMgr.Instance.CurrentYear);
		
		_nextYearButton = GetNode<Button>("NextYearButton");
		_nextYearButton.Pressed += () => TimeMgr.Instance.AdvanceYear();
		
		_moneyLabel = GetNode<Label>("MoneyLabel");
		MoneyMgr.Instance.MoneyChanged += _moneyUpdate;
		_moneyUpdate(MoneyMgr.Instance.CurrentMoney);
	}

	private void _yearUpdate(int year)
	{
		_yearLabel.Text = year.ToString();
	}

	private void _moneyUpdate(int money)
	{
		_moneyLabel.Text = $"￥{money}";
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

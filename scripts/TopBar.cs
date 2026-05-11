using Godot;
using System;

public partial class TopBar : HBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	private Label _yearLabel;
	private Button _nextYearButton;
	
	public override void _Ready()
	{		
		_yearLabel = GetNode<Label>("YearLabel");
		TimeMgr.Instance.YearAdvanced += _yearUpdate;
		_yearUpdate(TimeMgr.Instance.InitialYear);
		_nextYearButton = GetNode<Button>("NextYearButton");
		_nextYearButton.Pressed += () => TimeMgr.Instance.AdvanceYear();
	}

	private void _yearUpdate(int year)
	{
		_yearLabel.Text = year.ToString();
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

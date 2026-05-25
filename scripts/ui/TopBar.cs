using Godot;
using System;

public partial class TopBar : Control
{
	// Called when the node enters the scene tree for the first time.
	private Label _yearLabel;
	private Label _nextGoalLabel;
	private Button _nextYearButton;
	
	public override void _Ready()
	{		
		_yearLabel = GetNode<Label>("YearPanel/YearRow/YearLabel");
		_nextGoalLabel = GetNode<Label>("GoalPanel/GoalRow/NextGoalLabel");
		TimeMgr.Instance.YearAdvanced += _yearUpdate;
		_yearUpdate(TimeMgr.Instance.InitialYear);
		_nextYearButton = GetNode<Button>("YearPanel/YearRow/NextYearButton");
		_nextYearButton.Pressed += () => TimeMgr.Instance.EndYear();
		_nextYearButton.MouseEntered += () => AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.UiHover);
	}

	private void _yearUpdate(int year)
	{
		_yearLabel.Text = year.ToString();
		UpdateNextGoal(year);
	}

	private void UpdateNextGoal(int year)
	{
		var mileStone = MileStoneDatabase.GetNext(year + 1);
		_nextGoalLabel.Text = mileStone == null
			? "已完成所有目标"
			: $"下一目标: {mileStone.Year}年 / 声誉 {mileStone.TargetScore}";
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

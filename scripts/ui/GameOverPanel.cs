using Godot;
using System;

public partial class GameOverPanel : PanelContainer
{
	[Export] private Label _label;
	[Export] private Button _mainMenuButton;
	[Export] private Button _replayButton;

	public string Text
	{
		set => _label.Text = value;
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Visible = false;
		ScoreMgr.Instance.GameCompleted += () =>
		{
			Visible = true;
			Text = "成功";
			GetTree().Paused = true;
		};
		ScoreMgr.Instance.GameFailed += () => {
			Visible = true;
			Text = "失败";
			GetTree().Paused = true;
		};
		_mainMenuButton.Pressed += OnMainMenuButtonPressed;
		_replayButton.Pressed += OnReplayButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnMainMenuButtonPressed()
	{
		
	}

	private void OnReplayButtonPressed()
	{
		GetTree().Paused = false;
		GameMgr.Instance.RestartGame();
	}
}

using Godot;
using System;

public partial class ScoreBoard : PanelContainer
{
	public static ScoreBoard Instance { get; private set; }
	public float Reputation
	{
		set => _repLabel.Text = $"{value:F2}";
	}
	public float Research
	{
		set => _resLabel.Text = $"{value:F2}";
	}
	public float Education
	{
		set => _eduLabel.Text = $"{value:F2}";
	}
	public float Environment
	{
		set => _envLabel.Text = $"{value:F2}";
	}
	public float Population
	{
		set => _popLabel.Text = $"{value:F2}";
	}
	public float ResWeight
	{
		set => _resWeightLabel.Text = $"{value:F2}";
	}
	public float EduWeight
	{
		set => _eduWeightLabel.Text = $"{value:F2}";
	}
	public float EnvWeight
	{
		set => _envWeightLabel.Text = $"{value:F2}";
	}
	
	[Export] private Label _repLabel;
	[Export] private Label _resLabel;
	[Export] private Label _eduLabel;
	[Export] private Label _envLabel;
	[Export] private Label _popLabel;
	[Export] private Label _moneyLabel;
	[Export] private Label _resWeightLabel;
	[Export] private Label _eduWeightLabel;
	[Export] private Label _envWeightLabel;
	
	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if(Instance == this)
			Instance = null;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitializeLabels();
		MoneyMgr.Instance.MoneyChanged += _updateMoney;
		ScoreMgr.Instance.ScoreChanged += _updateScore;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	private void InitializeLabels()
	{
	// 	_repLabel = GetNode<Label>("VBoxContainer/RepBoard/Score");
	// 	_resLabel = GetNode<Label>("VBoxContainer/ResBoard/Score");
	// 	_eduLabel = GetNode<Label>("VBoxContainer/EduBoard/Score");
	// 	_envLabel = GetNode<Label>("VBoxContainer/EnvBoard/Score");
	// 	_popLabel = GetNode<Label>("VBoxContainer/PopBoard/Score");
	// 	_resWeightLabel = GetNode<Label>("VBoxContainer/ResBoard/Weight");
	// 	_eduWeightLabel = GetNode<Label>("VBoxContainer/EduBoard/Weight");
	// 	_envWeightLabel = GetNode<Label>("VBoxContainer/EnvBoard/Weight");
		InitializeScores();
		return;
	}
	
	
	private void InitializeScores()
	{
		Reputation = 0;
		Research = 0;
		Education = 0;
		Environment = 0;
		Population = 0;
		ResWeight = 0;
		EduWeight = 0;
		EnvWeight = 0;
		return;
	}
	
	private void _updateMoney(float amount)
	{
		_moneyLabel.Text = $"${amount:F2}";
	}

	private void _updateScore()
	{
		_repLabel.Text = $"{ScoreMgr.Instance.Reputation:F2}";
		_resLabel.Text = $"{ScoreMgr.Instance.Research:F2}";
		_eduLabel.Text = $"{ScoreMgr.Instance.Education:F2}";
		_envLabel.Text = $"{ScoreMgr.Instance.Environment:F2}";
		_popLabel.Text = $"{ScoreMgr.Instance.Population:F2}";
		_resWeightLabel.Text = $"{ScoreMgr.Instance.ResWeight:F2}";
		_eduWeightLabel.Text = $"{ScoreMgr.Instance.EduWeight:F2}";
		_envWeightLabel.Text = $"{ScoreMgr.Instance.EnvWeight:F2}";
	}


	
}
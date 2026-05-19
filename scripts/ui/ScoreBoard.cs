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

	public float Money
	{
		set => _moneyLabel.Text = $"{value:F2}";
	}
	public float TotalBenefit
	{
		set => _totalBenLabel.Text = $"{value:F2}";
	}
	public float BuildingBenefit
	{
		set => _buildBenLabel.Text = $"{value:F2}";
	}

	public float Tuition
	{
		set => _tuiLabel.Text = $"{value:F2}";
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
	[Export] private Label _tuiLabel;
	[Export] private Label _totalBenLabel;
	[Export] private Label _buildBenLabel;
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
		InitializeScores();
		MoneyMgr.Instance.MoneyChanged += _updateScore;
		ScoreMgr.Instance.ScoreChanged += _updateScore;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	
	
	private void InitializeScores()
	{
		Reputation = 0;
		Research = 0;
		Education = 0;
		Environment = 0;
		Population = 0;

		ResWeight = 1;
		EduWeight = 1;
		EnvWeight = 1;

		Money = MoneyMgr.Instance.InitialMoney;
		Tuition = MoneyMgr.Instance.Tuition;
		TotalBenefit = 0;
		BuildingBenefit = 0;
		return;
	}

	private void _updateScore()
	{
		var scoreCtx = ScoreMgr.Instance.ScoreContext;
		Reputation = scoreCtx.Reputation;
		Research = scoreCtx.Research;
		Education = scoreCtx.Education;
		Environment = scoreCtx.Environment;
		Population = scoreCtx.Population;
		Money = MoneyMgr.Instance.CurrentMoney;
		Tuition = MoneyMgr.Instance.Tuition;
		TotalBenefit = scoreCtx.Benefit;
		BuildingBenefit = scoreCtx.BuildingBen;
		ResWeight = scoreCtx.ResWeight;
		EduWeight = scoreCtx.EduWeight;
		EnvWeight = scoreCtx.EnvWeight;
	}


	
}
using Godot;
using System;
using System.Linq;

public class Weights
{
	public float ResWeight = 1f;
	public float EduWeight = 1f;
	public float EnvWeight = 1f;
}
public class Scores
{
	public float Research = 0f;
	public float Education = 0f;
	public float Environment = 0f;
	public float Population = 0f;
	public float Benefit = 0f;
}

public class ScoreContext
{
	private Scores _baseScores;
	private Scores _extraScores;
	private Weights _weights;

	public ScoreContext(Scores baseScores=null, Scores extraScores=null, Weights weights=null)
	{
		_baseScores = baseScores ?? new Scores();
		_extraScores = extraScores ?? new Scores();
		_weights = weights ?? new Weights();
	}
	public float BuildingBen => _baseScores.Benefit;
	public float Benefit => _baseScores.Benefit + _extraScores.Benefit + Population * MoneyMgr.Instance.Tuition;
	public float Research => _baseScores.Research + _extraScores.Research;
	public float ResWeight => _weights.ResWeight;
	public float Education => _baseScores.Education + _extraScores.Education;
	public float EduWeight => _weights.EduWeight;
	public float Environment => _baseScores.Environment + _extraScores.Environment;
	public float EnvWeight => _weights.EnvWeight;
	public float Population => _baseScores.Population + _extraScores.Population;
	public float Reputation => Research * ResWeight + Education * EduWeight + Environment * EnvWeight;
}
public partial class ScoreMgr : Node
{
	public static ScoreMgr Instance { get; private set; }

	public ScoreContext ScoreContext
	{
		get => _scoreContext;
		set
		{
			_scoreContext = value;
			EmitSignalScoreChanged();
		}
	}
	private ScoreContext _scoreContext;

	public Weights Weights => RecomputeScoreWeights();
	
	[Signal] public delegate void ScoreChangedEventHandler();
	[Signal] public delegate void GameCompletedEventHandler();
	[Signal] public delegate void GameFailedEventHandler();

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
		BuildingMgr.Instance.SpawnedBuilding += _ => RecomputeScores();
		BuildingMgr.Instance.DestroyedBuilding += RecomputeScores;
		BuffMgr.Instance.BuffsChanged += RecomputeScores;
		TimeMgr.Instance.YearAdvanced += CheckMileStones;
		InitializeScores();
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void InitializeScores()
	{
		ScoreContext = new ScoreContext();
		return;
	}


	private void RecomputeScores()
	{
		var spawnedBuildings = BuildingMgr.Instance.SpawnedBuildings;
		var baseScores = new Scores();
		foreach (var building in spawnedBuildings.SelectMany(pair => pair.Value))
		{
			baseScores.Research += building.Research;
			baseScores.Education += building.Education;
			baseScores.Environment += building.Environment;
			baseScores.Population += building.Population;
			baseScores.Benefit += building.Benefit;
		}
		var weights = RecomputeScoreWeights();
		var extraScores = RecomputeExtraScores(baseScores);
		ScoreContext = new ScoreContext(baseScores, extraScores, weights);
		return;
	}
	public Weights RecomputeScoreWeights()
	{
		var weight = new Weights();

		if (BuffMgr.Instance != null)
			BuffMgr.Instance.ApplyAllWeightBuffs(weight);

		EmitSignalScoreChanged();
		return weight;
	}
	
	private Scores RecomputeExtraScores(Scores baseScores)
	{
		var extraScores = new Scores();
		var buffs = BuffMgr.Instance.GetScoreBuffs();
		foreach (var activeBuff in buffs.Where(buff=>buff.IsActive()))
		{
			float srcScore = activeBuff.Args.ConditionStat switch
			{
				"Research" => baseScores.Research,
				"Education" => baseScores.Education,
				"Environment" => baseScores.Environment,
				"Population" => baseScores.Population,
				_ => throw new Exception($"未知的评分来源: {activeBuff.Args.ConditionStat}")
			};
			var multiplier = activeBuff.Args.Value / 100f;
			foreach (var target in activeBuff.Args.Targets)
			{
				switch (target)
				{
					case "Research":
						extraScores.Research += srcScore * multiplier;
						break;
					case "Education":
						extraScores.Education += srcScore * multiplier;
						break;
					case "Environment":
						extraScores.Environment += srcScore * multiplier;
						break;
					case "Population":
						extraScores.Population += srcScore * multiplier;
						break;
					case "Benefit":
						extraScores.Benefit += srcScore * multiplier;
						break;
					default:
						throw new Exception($"未知的评分目标: {target}");
				}
			}
		}
		return extraScores;
	}

	private void CheckMileStones(int newYear)
	{
		var mileStone = MileStoneDatabase.GetNext(newYear);
		if (mileStone == null)
		{
			throw new Exception("无尽头模式");
		}
		
		if (mileStone.Year != newYear) return;
		
		if(ScoreContext.Reputation >= mileStone.TargetScore)
		{
			GD.Print($"达成里程碑: {mileStone.Year}年, 目标声誉: {mileStone.TargetScore}");
			if(MileStoneDatabase.IsLast(mileStone.Year))
			{
				GD.Print("游戏胜利");
				EmitSignalGameCompleted();
			}
		}
		else
		{
			GD.Print($"未达成里程碑: {mileStone.Year}年, 目标声誉: {mileStone.TargetScore}, 当前声誉: {ScoreContext.Reputation}");
			GD.Print("游戏失败");
			EmitSignalGameFailed();
		}

		return;
	}
}

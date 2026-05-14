using Godot;
using System;
using System.Linq;

public partial class ScoreMgr : Node
{
	public static ScoreMgr Instance { get; private set; }
	public float Reputation => Research * ResWeight + Education * EduWeight + Environment * EnvWeight;
	public float Research => ResBase;
	public float ResBase
	{
		get => _resBase;
		set
		{
			_resBase = value;
			EmitSignalScoreChanged();
		}
	}
	private float _resBase = 0;
	public float Education => EduBase;
	public float EduBase
	{
		get => _eduBase;
		set
		{
			_eduBase = value;
			EmitSignalScoreChanged();
		}
	}
	private float _eduBase = 0;
	public float Environment => EnvBase;
	public float EnvBase
	{
		get => _envBase;
		set
		{
			_envBase = value;
			EmitSignalScoreChanged();
		}
	}
	private float _envBase=0;

	public float Population
	{
		get => _population;
		set
		{
			_population = value;
			EmitSignalScoreChanged();
		}
	}
	private float _population=0;
	public float ResWeight
	{
		get { return _resWeight; }
		set
		{
			_resWeight = value;
			EmitSignal(SignalName.ScoreChanged);
		}
	}
	private float _resWeight=1;
	public float EduWeight
	{
		get { return _eduWeight; }
		set
		{
			_eduWeight = value;
			EmitSignal(SignalName.ScoreChanged);
		}
	}
	private float _eduWeight=1;
	public float EnvWeight
	{
		get { return _envWeight; }
		set
		{
			_envWeight = value;
			EmitSignal(SignalName.ScoreChanged);
		}
	}
	private float _envWeight=1;
	
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
		Population = _population;
		ResWeight = _resWeight;
		EduWeight = _eduWeight;
		EnvWeight = _envWeight;
		return;
	}


	private void RecomputeScores()
	{
		var spawnedBuildings = BuildingMgr.Instance.SpawnedBuildings;
		_resBase = 0f;
		_eduBase = 0f;
		_envBase = 0f;
		_population = 0f;
		foreach (var building in spawnedBuildings.SelectMany(pair => pair.Value))
		{
			_resBase += building.Research;
			_eduBase += building.Education;
			_envBase += building.Environment;
			_population += building.Population;
		}
		RecomputeScoreWeights();
	}
	public void RecomputeScoreWeights()
	{
		var context = new ScoreContext();

		if (BuffMgr.Instance != null)
			BuffMgr.Instance.ApplyWeightBuffs(context);

		_resWeight = context.ResWeight;
		_eduWeight = context.EduWeight;
		_envWeight = context.EnvWeight;

		EmitSignalScoreChanged();
	}

	private void UpdateScores(BuildingBase building)
	{
		_resBase += building.Research;
		_eduBase += building.Education;
		_envBase += building.Environment;
		_population += building.Population;
		EmitSignal(SignalName.ScoreChanged);
	}

	private void CheckMileStones(int newYear)
	{
		var mileStone = MileStoneDatabase.GetNext(newYear);
		if (mileStone == null)
		{
			throw new Exception("无尽头模式");
		}
		
		if (mileStone.Year != newYear) return;
		
		if(Reputation >= mileStone.TargetScore)
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
			GD.Print($"未达成里程碑: {mileStone.Year}年, 目标声誉: {mileStone.TargetScore}, 当前声誉: {Reputation}");
			GD.Print("游戏失败");
			EmitSignalGameFailed();
		}

		return;
	}
}

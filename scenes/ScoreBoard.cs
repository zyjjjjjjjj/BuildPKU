using Godot;
using System;

public partial class ScoreBoard : PanelContainer
{
	private float Reputation
	{
		get { return _reputation; }
		set
		{
			_reputation = value;
			_repLabel.Text = $"{_reputation:F2}";
		}
	}
	private float _reputation=0;
	private float Research
	{
		get { return _research; }
		set
		{
			_research = value;
			_resLabel.Text = $"{_research:F2}";
		}
	}
	private float _research=0;
	private float _resBase = 0;
	private float Education
	{
		get { return _education; }
		set
		{
			_education = value;
			_eduLabel.Text = $"{_education:F2}";
		}
	}
	private float _education=0;
	private float _eduBase = 0;
	private float Environment
	{
		get { return _environment; }
		set
		{
			_environment = value;
			_envLabel.Text = $"{_environment:F2}";
		}
	}
	private float _environment=0;
	private float _envBase=0;

	private float Population
	{
		get { return _population; }
		set
		{
			_population = value;
			_popLabel.Text = $"{_population:F2}";
		}
	}
	private float _population=0;

	private float ResWeight
	{
		get { return _resWeight; }
		set
		{
			_resWeight = value;
			_resWeightLabel.Text = $"{_resWeight:F2}";
		}
	}
	private float _resWeight=1;
	private float EduWeight
	{
		get { return _eduWeight; }
		set
		{
			_eduWeight = value;
			_eduWeightLabel.Text = $"{_eduWeight:F2}";
		}
	}
	private float _eduWeight=1;
	private float EnvWeight
	{
		get { return _envWeight; }
		set
		{
			_envWeight = value;
			_envWeightLabel.Text = $"{_envWeight:F2}";
		}
	}
	private float _envWeight=1;
	
	private BuildingMgr _buildingMgr;
	private Label _repLabel;
	private Label _resLabel;
	private Label _eduLabel;
	private Label _envLabel;
	private Label _popLabel;
	private Label _moneyLabel;
	
	private Label _resWeightLabel;
	private Label _eduWeightLabel;
	private Label _envWeightLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitializeLabels();
		_buildingMgr = GetTree().CurrentScene.GetNode<BuildingMgr>("buildingMgr");
		if (_buildingMgr == null)
		{
			throw new Exception("BuildingMgr not found");
		}
		_buildingMgr.SpawnedBuilding += UpdateScores;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	private void InitializeLabels()
	{
		_repLabel = GetNode<Label>("VBoxContainer/RepBoard/Score");
		_resLabel = GetNode<Label>("VBoxContainer/ResBoard/Score");
		_eduLabel = GetNode<Label>("VBoxContainer/EduBoard/Score");
		_envLabel = GetNode<Label>("VBoxContainer/EnvBoard/Score");
		_popLabel = GetNode<Label>("VBoxContainer/PopBoard/Score");
		_resWeightLabel = GetNode<Label>("VBoxContainer/ResBoard/Weight");
		_eduWeightLabel = GetNode<Label>("VBoxContainer/EduBoard/Weight");
		_envWeightLabel = GetNode<Label>("VBoxContainer/EnvBoard/Weight");
		InitializeScores();
		return;
	}

	private void InitializeScores()
	{
		Reputation = _reputation;
		Research = _research;
		Education = _education;
		Environment = _environment;
		Population = _population;
		ResWeight = _resWeight;
		EduWeight = _eduWeight;
		EnvWeight = _envWeight;
		return;
	}

	public void RecomputeScores()
	{
		CollectBaseProps();
		CalculateScore();
		return;
	}
	
	public void UpdateScores(BuildingBase building)
	{
		_resBase += building.Research;
		_eduBase += building.Education;
		_envBase += building.Environment;
		_population += building.Population;
		CalculateScore();
		GD.Print(building.Research);
		GD.Print(building.Education);
		GD.Print(building.Environment);
	}
	private void CollectBaseProps()
	{
		var spawnedBuilding = _buildingMgr.SpawnedBuildings;
		_resBase = 0f;
		_eduBase = 0f;
		_envBase = 0f;
		_population = 0f;
		foreach (var building in spawnedBuilding)
		{
			_resBase += building.Research;
			_eduBase += building.Education;
			_envBase += building.Environment;
			_population += building.Population;
		}
	}
	private void CalculateScore()
	{
		Population = _population;
		Research = _resBase * Population;
		Education = _eduBase * Population;
		Environment = _envBase / (Population+1);
		Reputation = _research * _resWeight + _education * _eduWeight + _environment * _envWeight;
	}
	
}

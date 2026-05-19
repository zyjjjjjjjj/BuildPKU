using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BuildingBase : Node2D
{
	public string Id;
	public List<string> Tags = new List<string>();
	public string DisplayName; //display name, not unique
	public float _research = 0f;
	public float _education = 0f;
	public float _environment = 0f;
	public float _population = 0f;
	public float _benefit = 0f;
	private float _benMultiplier = 1f;
	private float _resMultiplier = 1f;
	private float _eduMultiplier = 1f;
	private float _envMultiplier = 1f;
	private float _popMultiplier = 1f;
	public float Benefit => _benefit * _benMultiplier;
	public float Population => _population * _popMultiplier;
	public float Research => _research * _resMultiplier;
	public float Education => _education * _eduMultiplier;
	public float Environment => _environment * _envMultiplier;


	private int _rotationStep = 0;
	public int RotationStep
	{
		get => _rotationStep;
		set
		{
			_rotationStep = value % 4;
			if (_rotationStep < 0)
				_rotationStep += 4;
			Rotation = _rotationStep * Mathf.Pi / 2f;
			_currentShape = null;
		}
	}
	public Vector2 GridOriginGlobalPos
	{
		get
		{
			var gridPos = _getGridNode().GlobalPosition;
			return gridPos;
		}
	}

	public BuildingGrid BuildingGridNode => _getGridNode();
	public Vector2I GridSize => BuildingGridNode.GridSize;

	private List<Vector2I> _currentShape;
	public List<Vector2I> CurrentShape
	{
		get
		{
			if (_currentShape != null)
				return _currentShape;
			_currentShape = new List<Vector2I>();
			for(int idx=0;idx<GridSize.X*GridSize.Y;idx++)
			{
				if (!BuildingGridNode.ShapeMask[idx]) continue;
				var localIdx = new Vector2I(idx % GridSize.X, idx / GridSize.X);
				localIdx = _rotate90Clockwise(localIdx, _rotationStep);
				_currentShape.Add(localIdx);
			}
			return _currentShape;
		}
	}

	private List<ActiveBuff> _relatedBuffs = [];
	public List<ActiveBuff> RelatedBuffs {
		get
		{
			return _relatedBuffs;
		}
		private set => _relatedBuffs = value;
	}

	public bool Destroyable;
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var scale = GridMgr.Instance.CellSize / BuildingGridNode.CellSize;
		_setUpCollision();
		Scale = new Vector2(scale, scale);
		Rotation = 0f;
	}

	private void _setUpCollision()
	{
		var grid = _getGridNode();
		var curShape = CurrentShape;
		foreach (var offset in curShape)
		{
			var collisionPoint = new Area2D();
			var collision = new CollisionShape2D();
			var shape = new CircleShape2D();
			shape.Radius = 1f;
			collision.Shape = shape;
			collisionPoint.AddChild(collision);
			collisionPoint.Position = (new Vector2(0.5f,0.5f) +offset) * BuildingGridNode.CellSize;
			collisionPoint.AddToGroup("tmpBuilding");
			grid.AddChild(collisionPoint);
		}
	}

	private BuildingGrid _getGridNode()
	{
		var grid = GetNodeOrNull<BuildingGrid>("grid");
		if(grid == null)
			throw new InvalidOperationException("未找到 grid 节点，请检查节点路径");
		return grid;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void RotateBuilding(bool clockwise)
	{
		if (clockwise)
		{
			RotationStep++;
		}
		else
		{
			RotationStep--;
		}
	}
	private Vector2I _rotate90Clockwise(Vector2I v, int times=1)
	{
		while(times>0)
		{
			v = new Vector2I(v.Y, -v.X);
			times--;
		}
		return v;
	}
	
	public List<Area2D> GetCollisionAreas()
	{
		var areas = new List<Area2D>();
		foreach (var child in BuildingGridNode.GetChildren())
		{
			if (child is Area2D area)
			{
				areas.Add(area);
			}
		}
		return areas;
	}
	
	public void InitializeFromData(BuildingData data)
	{
		DisplayName = data.Name;
		_research = data.Research;
		_education = data.Education;
		_environment = data.Environment;
		_population = data.Population;
		_benefit = data.Benefit; 
		Destroyable = data.Destroyable;
		Tags = data.Tags;
		return;
	}
	
	public void ApplyAllBuildingBuffs()
	{
		var buffs = BuffMgr.Instance.GetRelatedBuffs(Id,Tags);
		foreach (var buff in buffs.Where(buff=>buff.IsActive()))
			ApplyBuff(buff);
	}
	
	public void ApplyAdjBonus(ActiveBuff buff)
	{
		if(buff.Data.Id != "adj_bonus_buff")
			return;
		var neighbors = GridMgr.Instance.GetNeighborBuildingIds(this);
		var idOrTag = buff.Args.SrcBuildingIdOrTag;
		if (BuildingDatabase.IsId(idOrTag) && !neighbors.Contains(idOrTag))
			return;
		if (!BuildingDatabase.IsId(idOrTag) && !ContainNeighborWithTag(idOrTag, neighbors))
			return;
		_relatedBuffs.Add(buff);
		ApplyMultiplier(buff.Args.Targets, buff.Args.Value);
	}
	
	private void ApplyPropBuff(ActiveBuff buff)
	{
		if(buff.Data.Id != "basic_prop_buff")
			return;
		_relatedBuffs.Add(buff);
		ApplyMultiplier(buff.Args.Targets, buff.Args.Value);
	}
	
	public void ApplyBuff(ActiveBuff buff)
	{
		switch (buff.Data.Id)
		{
			case "basic_prop_buff":
				ApplyPropBuff(buff);
				break;
			case "adj_bonus_buff":
				ApplyAdjBonus(buff);
				break;
		}
	}

	public void RecomputeProps()
	{
		_relatedBuffs.Clear();
		ResetMultiplier();
		ApplyAllBuildingBuffs();
	}

	private void ResetMultiplier()
	{
		_resMultiplier = 1f;
		_eduMultiplier = 1f;
		_envMultiplier = 1f;
		_popMultiplier = 1f;
		_benMultiplier = 1f;
		return;
	}
	
	private void ApplyMultiplier(List<string> targets, float value)
	{
		float multiplier = value / 100f;
		foreach (var target in targets)
		{
			switch (target)
			{
				case "Research":
					_resMultiplier *= multiplier;
					break;
				case "Education":
					_eduMultiplier *= multiplier;
					break;
				case "Environment":
					_envMultiplier *= multiplier;
					break;
				case "Population":
					_popMultiplier *= multiplier;
					break;
				case "Benefit":
					_benMultiplier *= multiplier;
					break;
			}
		}
	}
	
	private bool ContainNeighborWithTag(string tag, HashSet<string> neighborIds)
	{
		if (tag == String.Empty)
			return true;
		foreach (var id in neighborIds)
		{
			var buildingData = BuildingDatabase.GetById(id);
			if (buildingData.Tags.Contains(tag))
				return true;
		}
		return false;
	}
}

using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingBase : Node2D
{
	public string Id;
	public string BuildingName;
	public float _research = 0f;
	public float _education = 0f;
	public float _environment = 0f;
	public float _population = 0f;
	public float _cost = 0f;
	public float _benefit = 0f;
	private float _benMultiplier = 1f;
	private float _resMultiplier = 1f;
	private float _eduMultiplier = 1f;
	private float _envMultiplier = 1f;
	private float _popMultiplier = 1f;
	public float Cost => _cost;
	public float Benefit => _benefit;
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
		BuildingName = data.Name;
		_research = data.Research;
		_education = data.Education;
		_environment = data.Environment;
		_population = data.Population;
		_cost = data.Cost;
		_benefit = data.Benefit;
		 Destroyable = data.Destroyable;
		 return;
	}
	
	public void ApplyAdjBonus(ActiveBuff buff)
	{
		var buffData = buff.Data;
		if(buffData.BuffType != "AdjBonus")
			return;
		var neighbors = GridMgr.Instance.GetNeighborBuildingIds(this);
		if(!neighbors.Contains(buffData.SrcBuildingId))
			return;
		_relatedBuffs.Add(buff);
		ApplyMutiplier(buffData.Targets, buff.Value);
	}

	public void RecomputeAdjBonus()
	{
		_relatedBuffs.Clear();
		ResetMultiplier();
		var neighbors = GridMgr.Instance.GetNeighborBuildingIds(this);
		var adjBuffs = BuffMgr.Instance.GetAdjBuffsById(Id);
		foreach (var adjBuff in adjBuffs)
		{
			if (neighbors.Contains(adjBuff.Data.SrcBuildingId))
			{
				_relatedBuffs.Add(adjBuff);
				ApplyMutiplier(adjBuff.Data.Targets, adjBuff.Value);
			}
		}
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
	
	private void ApplyMutiplier(List<string> targets, int value)
	{
		float multiplier = (100f + value) / 100f;
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
}


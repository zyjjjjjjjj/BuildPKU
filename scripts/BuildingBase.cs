using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingBase : Node2D
{
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
			// switch (_rotationStep)
			// {
			// 	case 0:
			// 		break;
			// 	case 1:
			// 		gridPos.X -= GridSize.Y * GameConfig.Instance.CellSize;
			// 		break;
			// 	case 2:
			// 		gridPos.X -= GridSize.X * GameConfig.Instance.CellSize;
			// 		gridPos.Y += GridSize.Y * GameConfig.Instance.CellSize;
			// 		break;
			// 	case 3:
			// 		gridPos.Y += GridSize.X * GameConfig.Instance.CellSize;
			// 		break;
			// }
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
	
	private GridMgr _gridMgr;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_gridMgr = GetTree().CurrentScene.GetNode<GridMgr>("gridMgr");
		if(_gridMgr == null)
			throw new Exception("GridMgr not found");
		var scale = _gridMgr.CellSize / BuildingGridNode.CellSize;
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
		foreach (var child in GetNode<BuildingGrid>("grid").GetChildren())
		{
			if (child is Area2D area)
			{
				areas.Add(area);
			}
		}
		return areas;
	}
}


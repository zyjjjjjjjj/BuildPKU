using Godot;
using System;

public partial class BuildingBase : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_setUpCollision();
	}

	private void _setUpCollision()
	{
		GD.Print("setUpCollision");
		var grid = _getGridNode();
		for(int i=0;i<grid.GridSize.X;i++)
		{
			for (int j = 0; j < grid.GridSize.Y; j++)
			{
				if (!grid.ShapeMask[i]) continue;
				var collisionPoint = new Area2D();
				var collision = new CollisionShape2D();
				var shape = new CircleShape2D();
				shape.Radius = 0.01f;
				collision.Shape = shape;
				collisionPoint.AddChild(collision);
				collisionPoint.Position = new Vector2((0.5f+i) * grid.CellSize, (0.5f+j) * grid.CellSize);
				collisionPoint.AddToGroup("tmpBuilding");
				grid.AddChild(collisionPoint);
			}
		}
	}

	private Grid _getGridNode()
	{
		var grid = GetNodeOrNull<Grid>("grid");
		if(grid == null)
			throw new InvalidOperationException("未找到 grid 节点，请检查节点路径");
		return grid;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

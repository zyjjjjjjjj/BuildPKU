using System;
using Godot;

public partial class CellArea : Area2D
{
    public Vector2I Idx;
    public bool HighlightVisible = false;
    public Color HighlightColor = new Color(0, 1, 0, 0.35f);
    public override void _Ready()
    {
        ZIndex = 100;
        _setUpCollision();
        MouseEntered += () => BuildingInfoPanel.Instance.RefreshPanel();
        MouseExited += () => BuildingInfoPanel.Instance.RefreshPanel();
    }

    private void _setUpCollision()
    {
        var collision = new CollisionShape2D();
        var shape = new RectangleShape2D();
        var cellSize = GridMgr.Instance.CellSize;
        shape.Size = new Vector2(cellSize - 0.1f, cellSize - 0.1f);
        collision.Shape = shape;
        collision.Position = new Vector2(cellSize / 2f, cellSize / 2f);
        AddChild(collision);
    }
    
    public enum CellState
    {
        Occupied,
        Conflict,
        Available,
        Null,
    }

    public override void _Draw()
    {
        if (!HighlightVisible)
            return;

        DrawRect(
            new Rect2(Vector2.Zero, new Vector2(GridMgr.Instance.CellSize, GridMgr.Instance.CellSize)),
            HighlightColor,
            true
        );
    }

    public void SetHighlight(CellState state)
    {
        switch (state)
        {
            case CellState.Occupied:
                HighlightColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                HighlightVisible = true;
                break;
            case CellState.Conflict:
                HighlightColor = new Color(1f, 0, 0, 0.35f);
                HighlightVisible = true;
                break;
            case CellState.Available:
                HighlightColor = new Color(0, 1, 0, 0.35f);
                HighlightVisible = true;
                break;
            case CellState.Null:
                HighlightVisible = false;
                break;
        }
        QueueRedraw();
    }
}
using Godot;

public partial class CellArea : Area2D
{
    public bool HighlightVisible = false;
    public Color HighlightColor = new Color(0, 1, 0, 0.35f);
    public int CellSize = 64;

    public CellArea()
    {
    }

    public CellArea(int cellSize)
    {
        CellSize = cellSize;
    }
    
    public override void _Ready()
    {
        base._Ready();
        ZIndex = 100;
        _setUpCollision();
    }

    private void _setUpCollision()
    {
        var collision = new CollisionShape2D();
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(CellSize - 0.1f, CellSize - 0.1f);
        collision.Shape = shape;
        collision.Position = new Vector2(CellSize / 2f, CellSize / 2f);
        AddChild(collision);
    }
    
    public enum CellState
    {
        Occupied,
        Conflict,
        Empty,
        Null,
    }

    public override void _Draw()
    {
        if (!HighlightVisible)
            return;

        DrawRect(
            new Rect2(Vector2.Zero, new Vector2(CellSize, CellSize)),
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
            case CellState.Empty:
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
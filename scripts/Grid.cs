using Godot;
using Godot.Collections;

[Tool]
public partial class Grid : Node2D
{
    private Vector2I _gridSize = new(1, 1);
    [Export]
    public Vector2I GridSize
    {
        get => _gridSize;
        set
        {
            _gridSize = value;
            QueueRedraw();
            ResizeShapeMask();
        }
    }

    private Array<bool> _shapeMask;

    [Export]
    public Array<bool> ShapeMask
    {
        get => _shapeMask;
        set
        {
            _shapeMask = value;
            ResizeShapeMask();
            QueueRedraw();
        }
    }

    private int _cellSize = 64;
    [Export] 
    public int CellSize
    {
        get => _cellSize;
        set
        {
            _cellSize = Mathf.Max(1, value);
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        QueueRedraw();
    }
    
    private void ResizeShapeMask()
    {
        int targetCount = GridSize.X * GridSize.Y;
        if (_shapeMask == null)
        {
            _shapeMask = new Array<bool>();
        }
        while (_shapeMask.Count < targetCount)
        {
            _shapeMask.Add(true);
        }
        while (_shapeMask.Count > targetCount)
        {
            _shapeMask.RemoveAt(_shapeMask.Count - 1);
        }
        NotifyPropertyListChanged();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint())
            return;

        for (var x = 0; x <= _gridSize.X; x++)
        {
            var from = new Vector2(x * _cellSize, 0);
            var to = new Vector2(x * _cellSize, _gridSize.Y * _cellSize);
            DrawLine(from, to, Colors.Gray, 3);
        }

        for (var y = 0; y <= _gridSize.Y; y++)
        {
            var from = new Vector2(0, y * _cellSize);
            var to = new Vector2(_gridSize.X * _cellSize, y * _cellSize);
            DrawLine(from, to, Colors.Gray, 3);
        }
        
        DrawMaskCells();
    }
    
    private void DrawMaskCells()
    {
        for (int y = 0; y < _gridSize.Y; y++)
        {
            for (int x = 0; x < _gridSize.X; x++)
            {
                int index = y * _gridSize.X + x;
                bool occupied = _shapeMask[index];
                if(!occupied)
                    continue;
                var rect = new Rect2(
                    new Vector2(x * _cellSize, y * _cellSize),
                    new Vector2(_cellSize, _cellSize)
                );
                DrawRect(rect, new Color(1.0f,0,0,0.3f), true);
            }
        }
    }

}
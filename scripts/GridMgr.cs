using System;
using Godot;
using System.Collections.Generic;

[Tool]
public partial class GridMgr : Node2D
{
    private int _gridWidth = 8;
    private int _gridHeight = 6;
    private int _cellSize = 64;
    private int _boundary = 2;
    private bool _showGrid = true;
    private Dictionary<Vector2I, CellArea> _idx2area = new Dictionary<Vector2I, CellArea>();
    private Dictionary<CellArea, Vector2I> _area2idx = new Dictionary<CellArea, Vector2I>();
    private bool _refreshPreviewScheduled = false;
    private readonly Dictionary<Vector2I, string> _occupied = new();
    private BuildingMgr _buildingMgr;
    private BuildingMgr BuildingMgr
    {
        get
        {
            if (Engine.IsEditorHint())
                return null;
            if (_buildingMgr == null)
                _buildingMgr = (BuildingMgr)GetNode("BuildingMgr");
            return _buildingMgr;
        }
    }
    private Camera2D _camera;
    private Camera2D Camera
    {
        get
        {
            if (Engine.IsEditorHint())
                return null;
            return _camera ??= GetViewport().GetCamera2D();
        }
    }
    [Export]
    public int GridWidth
    {
        get => _gridWidth;
        set
        {
            _gridWidth = Mathf.Max(1, value);
            QueueRedraw();
        }
    }
    [Export]
    public int GridHeight
    {
        get => _gridHeight;
        set
        {
            _gridHeight = Mathf.Max(1, value);
            QueueRedraw();
        }
    }
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
    [Export]
    public int Boundary
    {
        get => _boundary;
        set
        {
            _boundary = Mathf.Max(0, value);
            QueueRedraw();
        }
    }
    [Export]
    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            _showGrid = value;
            QueueRedraw();
        }
    }
    public override void _Ready()
    {
        _buildingMgr = GetTree().CurrentScene.GetNode<BuildingMgr>("buildingMgr");
        if (_buildingMgr == null)
        {
            throw new InvalidOperationException("BuildingMgr 是 null，请检查节点路径");
        }
        _buildingMgr.SpawnedBuilding += _updateOccupation;
        if(Camera!=null)
            ((CameraCtrl)Camera).CameraZoom += QueueRedraw;
        if (!Engine.IsEditorHint())
        {
            CreateGrid();
        }
        ShowGrid = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_refreshPreviewScheduled) return;
        _refreshPreviewScheduled = false;
        QueueRedraw();
    }

    private void CreateGrid()
    {
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                CreateCell(new Vector2I(x, y));
                if(x<Boundary || y<Boundary || x>=GridWidth-Boundary || y>=GridHeight-Boundary)
                {
                    var idx = new Vector2I(x, y);
                    _occupied[idx] = "boundary";
                }
            }
        }
    }
    private void CreateCell(Vector2I gridIdx)
    {
        var area = new CellArea();
        area.Name = $"Cell_{gridIdx.X}_{gridIdx.Y}";
        area.Position = new Vector2(gridIdx.X * CellSize, gridIdx.Y * CellSize);
        area.AreaEntered += otherArea =>
        {
            OnCellAreaEntered(otherArea, area);
        };
        area.AreaExited += otherArea =>
        {
            OnCellAreaExited(otherArea, area);
        };
        AddChild(area);
        _idx2area.Add(gridIdx, area);
        _area2idx.Add(area, gridIdx);
    }
    private void OnCellAreaEntered(Area2D otherArea, Area2D cellArea)
    {
        if (!otherArea.IsInGroup("tmpBuilding"))
            return;
        _queuePreviewRefresh();
    }
    private void OnCellAreaExited(Area2D otherArea, Area2D cellArea)
    {
        if (!otherArea.IsInGroup("tmpBuilding"))
            return;
        _queuePreviewRefresh();
    }
    private void _queuePreviewRefresh()
    {
        if(_refreshPreviewScheduled)
            return;
        _refreshPreviewScheduled = true;
        // CallDeferred(nameof(_refreshPreview));
    }
    private void _refreshPreview()
    {
        _clearPreview();
        if(!ShowGrid || BuildingMgr == null)
        {
            _refreshPreviewScheduled = false;
            return;
        }
        var tmpBuilding = BuildingMgr.GetNodeOrNull<BuildingBase>("tmpBuilding");
        if (tmpBuilding == null)
        {
            _refreshPreviewScheduled = false;
            return;
        }
        var collisionPoints = tmpBuilding.GetCollisionAreas();
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if (!_inBounds(idx))
                continue;
            _idx2area[idx].SetHighlight(_isOccupied(idx) ? CellArea.CellState.Conflict : CellArea.CellState.Empty);
        }
        _refreshPreviewScheduled = false;
    }
    private void _clearPreview()
    {
        foreach (var area in _area2idx)
        {
            if (!ShowGrid || !_isOccupied(area.Value))
            {
                area.Key.SetHighlight(CellArea.CellState.Null);
                continue;
            }
            area.Key.SetHighlight(CellArea.CellState.Occupied);
        }
    }
    public override void _Draw()
    {
        _refreshPreview();
        if (!ShowGrid)
            return;
        var lineWidth = 10f;
        if(Camera!=null)
            lineWidth = 2f / Camera.Zoom.X;
        for (int x = 0; x <= GridWidth; x++)
        {
            var from = new Vector2(x * CellSize, 0);
            var to = new Vector2(x * CellSize, GridHeight * CellSize);

            DrawLine(from, to, new Color(1f,1f,1f,0.5f), lineWidth);
        }
        for (int y = 0; y <= GridHeight; y++)
        {
            var from = new Vector2(0, y * CellSize);
            var to = new Vector2(GridWidth * CellSize, y * CellSize);

            DrawLine(from, to, new Color(1f,1f,1f,0.5f), lineWidth);
        }
    }
    private bool _inBounds(Vector2I idx)
    {
        return idx.X >= 0 && idx.X < GridWidth && idx.Y >= 0 && idx.Y < GridHeight;
    }
    private bool _isOccupied(Vector2I idx)
    {
        return _occupied.ContainsKey(idx);
    }
    public Vector2I WorldToGridIdx(Vector2 worldPos)
    {
        var localPos = worldPos - Position;
        localPos -= new Vector2(0.5f * CellSize, 0.5f * CellSize);
        int x = Mathf.RoundToInt(localPos.X / CellSize);
        int y = Mathf.RoundToInt(localPos.Y / CellSize);
        return new Vector2I(x, y);
    }

    public Vector2 GridIdxToWorld(Vector2I idx)
    {
        var localPos = new Vector2((idx.X + 0.5f) * CellSize, (idx.Y + 0.5f) * CellSize);
        return localPos + GlobalPosition;
    }
    public Vector2? CheckSpace(BuildingBase building)
    {
        var collisionPoints = building.GetCollisionAreas();
        Vector2? offset=null;
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if (!_inBounds(idx) || _isOccupied(idx))
                return null;
            offset = GridIdxToWorld(idx) - collisionPoint.GlobalPosition;
        }
        return offset;
    }
    private void _updateOccupation(BuildingBase newBuilding)
    {
        var collisionPoints = newBuilding.GetCollisionAreas();
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if (!_inBounds(idx) || _isOccupied(idx))
            {
                GD.PrintErr("尝试占用一个已经被占用的格子，或者越界了！请检查逻辑！");
            }
            _occupied[idx] = newBuilding.Name;
        }
        QueueRedraw();
    }
}
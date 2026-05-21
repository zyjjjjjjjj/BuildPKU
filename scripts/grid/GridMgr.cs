using System;
using Godot;
using System.Collections.Generic;

[Tool]
public partial class GridMgr : Node2D
{
    public static GridMgr Instance { get; private set; }
    private int _gridWidth = 8;
    private int _gridHeight = 6;

    public Vector2I GridSize => new(_gridWidth, _gridHeight);
    private int _cellSize = 64;
    private int _boundary = 2;
    private bool _showGrid = true;
    private Dictionary<Vector2I, CellArea> _idx2area = new Dictionary<Vector2I, CellArea>();
    private Dictionary<CellArea, Vector2I> _area2idx = new Dictionary<CellArea, Vector2I>();
    private bool _refreshPreviewScheduled = false;
    private readonly Dictionary<Vector2I, string> _occupied = new();
    private List<string> _allRegions = new List<string>();
    private List<string> _unlockedRegions = new List<string>();
    private Dictionary<Vector2I, string> _idx2regions = new();
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
    
    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if(Instance == this)
            Instance = null;
    }
    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        ShowGrid = false;
        BuildingMgr.Instance.EnterEditorMode += () =>
        {
            ShowGrid = true;
        };
        BuildingMgr.Instance.ExitEditorMode += () =>
        {            
            ShowGrid = false;
        };
        if(Camera!=null)
            ((CameraCtrl)Camera).CameraZoom += QueueRedraw;
        
        CreateGrid();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;
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
        area.Idx = gridIdx;
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
        if(!ShowGrid || BuildingMgr.Instance == null)
        {
            _refreshPreviewScheduled = false;
            return;
        }
        var tmpBuilding = BuildingMgr.Instance.GetNodeOrNull<BuildingBase>("tmpBuilding");
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
            _idx2area[idx].SetHighlight(_Buildable(idx) ? CellArea.CellState.Available : CellArea.CellState.Conflict);
        }
        _refreshPreviewScheduled = false;
    }
    private void _clearPreview()
    {
        foreach (var area in _area2idx)
        {
            if (!ShowGrid || _Buildable(area.Value))
            {
                area.Key.SetHighlight(CellArea.CellState.Null);
                continue;
            }
            area.Key.SetHighlight(CellArea.CellState.Occupied);
        }
    }
    public override void _Draw()
    {
        if(!Engine.IsEditorHint())
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
    
    private bool _inUnlockedRegion(Vector2I idx)
    {
        return !_idx2regions.TryGetValue(idx, out var regionName) || _unlockedRegions.Contains(regionName);
    }
    
    private bool _Buildable(Vector2I idx)
    {
        return _inBounds(idx) && !_isOccupied(idx) && _inUnlockedRegion(idx);
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
    public Vector2? CheckSpace(BuildingBase building, out int zIdx)
    {
        zIdx = GridHeight;
        if(building == null)
             return null;
        var collisionPoints = building.GetCollisionAreas();
        Vector2? offset=null;
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if (!_Buildable(idx))
                return null;
            offset = GridIdxToWorld(idx) - collisionPoint.GlobalPosition;
            if(idx.Y < zIdx)
                zIdx = idx.Y;
        }
        return offset;
    }
    public void UpdateOccupation(BuildingBase newBuilding, bool destroy = false)
    {
        var collisionPoints = newBuilding.GetCollisionAreas();
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if (destroy)
            {
                _occupied.Remove(idx);
            }
            else
            {
                if (!_inBounds(idx) || _isOccupied(idx))
                {
                    GD.PrintErr("尝试占用一个已经被占用的格子，或者越界了！请检查逻辑！");
                }
                _occupied[idx] = newBuilding.Name;
            }
        }
        QueueRedraw();
    }

    public List<string> GetNeighbors(BuildingBase building)
    {
        var collisionPoints = building.GetCollisionAreas();
        HashSet<Vector2I> self =  new HashSet<Vector2I>();
        List<string> neighbors = new List<string>();
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if(_inBounds(idx))
                self.Add(idx);
        }
        List<Vector2I> directions = new List<Vector2I>()
        {
            new Vector2I(1,0),
            new Vector2I(-1,0),
            new Vector2I(0,1),
            new Vector2I(0,-1),
        };
        foreach(var point in self)
        {
            foreach(var dir in directions)
            {
                var neighborIdx = point + dir;
                if(!self.Contains(neighborIdx) && _inBounds(neighborIdx) && _isOccupied(neighborIdx) && _occupied[neighborIdx]!="boundary")
                {
                    neighbors.Add(_occupied[neighborIdx]);
                }
            }
        }
        return neighbors;
    }

    public List<string> GetNeighbors(BuildingBase building, int radius)
    {
        var collisionPoints = building.GetCollisionAreas();
        HashSet<Vector2I> self =  new HashSet<Vector2I>();
        List<string> neighbors = new List<string>();
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if(_inBounds(idx))
                self.Add(idx);
        }
        foreach(var point in self)
        {
            for(int i=-radius;i<=radius;i++)
                for(int j=-radius;j<=radius;j++)
                {
                    var neighborIdx = point + new Vector2I(i,j);
                    if(!self.Contains(neighborIdx) && _inBounds(neighborIdx) && _isOccupied(neighborIdx) && _occupied[neighborIdx]!="boundary")
                    {
                        neighbors.Add(_occupied[neighborIdx]);
                    }
                }
        }
        return neighbors;
    }

    public string GetNameByIdx(Vector2I idx)
    {
        return _occupied.GetValueOrDefault(idx);
    }

    public void RegisterRegion(BuildingBase region)
    {
        _allRegions.Add(region.Name);
        var collisionPoints = region.GetCollisionAreas();
        foreach (var collisionPoint in collisionPoints)
        {
            var idx = WorldToGridIdx(collisionPoint.GlobalPosition);
            if (!_inBounds(idx))
                continue;
            _idx2regions[idx] = region.Name;
        }
        QueueRedraw();
    }
    
    public void UnlockRegion(string regionName)
    {
        if (_unlockedRegions.Contains(regionName))
            return;
        if (!_allRegions.Contains(regionName))
            return;
        _unlockedRegions.Add(regionName);
        QueueRedraw();
    }

    public string GetCurrentPointingBuildingName()
    {
        var mousePos = GetGlobalMousePosition();
        var idx = WorldToGridIdx(mousePos);
        if(!_inBounds(idx))            
            return null;
        return _occupied.GetValueOrDefault(idx);
    }
}
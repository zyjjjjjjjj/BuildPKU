using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;


public partial class BuildingMgr : Node2D
{
    public static BuildingMgr Instance { get; private set; }
    private string _selectedBuildingId = string.Empty;
    private BuildingBase _tmpBuilding;
    public readonly Dictionary<string, List<BuildingBase>> SpawnedBuildings = new();
    private int _spawningCnt = 0;
    private bool _isEditing = false;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            if (_isEditing)
            {
                EmitSignalEnterEditorMode();
                return;
            }
            _tmpBuilding?.QueueFree();
            _tmpBuilding = null;
            GD.Print("取消选择");
            EmitSignalExitEditorMode();
        }
    }
    public bool _upgrading = false;

    //!!!IMPORTANT: 需通过ModifyAvailableBuildingList方法修改，来保证信号正确发出
    public List<string> AvailableBuildingIds { get; private set; } = [];

    [Signal]
    public delegate void SpawnedBuildingEventHandler(BuildingBase building);
    [Signal]
    public delegate void DestroyedBuildingEventHandler();
    [Signal]
    public delegate void EnterEditorModeEventHandler();
    [Signal]
    public delegate void ExitEditorModeEventHandler();
    [Signal]
    public delegate void AvailableBuildingIdsChangedEventHandler(bool added, string buildingId);
    
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
        BuildingSideBar.Instance.SideBarBuildingSelected += SelectBuilding;
        AvailableBuildingIds = BuildingDatabase.GetAllIds();
    }

    public override void _Process(double delta)
    {
        if (_tmpBuilding == null)
            return;
        _tmpBuilding.GlobalPosition = GetLocalMousePosition();
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && IsEditing)
            {
                if (GetViewport().GuiGetHoveredControl() != null)
                    return;

                TrySpawnBuilding();
            }
        }
        
        if (@event is not InputEventKey keyEvent)
            return;

        if (!keyEvent.Pressed || keyEvent.Echo)
            return;

        switch (keyEvent.Keycode)
        {
            case Key.Q:
                _tmpBuilding?.RotateBuilding(false);
                break;
            case Key.E:
                _tmpBuilding?.RotateBuilding(true);
                break;
        }
    }

    private void SelectBuilding(string buildingId)
    {
        if(buildingId == string.Empty)
        {
            IsEditing = false;
            return;
        }
        IsEditing = true;
        _selectedBuildingId = buildingId;
        if (_tmpBuilding != null) _tmpBuilding.Name = "tmpBuilding_toBeFreed";
        _tmpBuilding?.QueueFree();
        var buildingScene = GD.Load<PackedScene>(BuildingDatabase.GetById(buildingId).ScenePath);
        _tmpBuilding = buildingScene.Instantiate<BuildingBase>();
        _tmpBuilding.GlobalPosition = GetLocalMousePosition();
        _tmpBuilding.Name = "tmpBuilding";
        _tmpBuilding.Modulate = _tmpBuilding.Modulate with { A = 0.5f };
        AddChild(_tmpBuilding);
    }

    public void TrySpawnBuilding(bool cost=true)
    {
        if (_tmpBuilding == null)
            return;

        var offset = GridMgr.Instance.CheckSpace(_tmpBuilding, out var zIdx);
        if (!offset.HasValue)
            return;

        var buildingData = BuildingDatabase.GetById(_selectedBuildingId);
        if (buildingData == null)
            return;

        if (cost && !MoneyMgr.Instance.TrySpend(buildingData.Cost))
        {
            WarningPanel.Instance.ShowWarning($"钱不够，需要 ￥{buildingData.Cost}");
            return;
        }

        SpawnBuilding(offset.Value + _tmpBuilding.Position, zIdx);
    }

    public void SpawnBuilding(Vector2 localPos, int zIdx)
    {
        var buildingData = BuildingDatabase.GetById(_selectedBuildingId);
        if (buildingData == null) 
            return;
        //实例化
        var buildingScene = GD.Load<PackedScene>(buildingData.ScenePath);
        var building = buildingScene.Instantiate<BuildingBase>();
        AddChild(building);
        building.Name = buildingData.Id + '_' + _spawningCnt++.ToString();
        building.ZIndex = zIdx;
        building.Position = localPos;
        building.Id = buildingData.Id;
        building.RotationStep = _tmpBuilding.RotationStep;
        building.InitializeFromData(buildingData);
        building.RecomputeAdjBonus();
        
        //更新信息
        if (SpawnedBuildings.TryGetValue(buildingData.Id, out var list))
        {
            list.Add(building);
        }
        else
            SpawnedBuildings[buildingData.Id] = [building];
        
        if(buildingData.Limit > 0 && SpawnedBuildings[buildingData.Id].Count >= buildingData.Limit)
            ModifyAvailableBuildingList(false, buildingData.Id);
        
        GridMgr.Instance.UpdateOccupation(building);
        
        //计算邻居相邻加成
        var neighbors = GridMgr.Instance.GetNeighbors(building);
        foreach (var name in neighbors)
            GetNode<BuildingBase>(name).RecomputeAdjBonus();
        
        EmitSignalSpawnedBuilding(building);
        if (_upgrading)
        {
            IsEditing = false;
            _upgrading = false;
        }
    }

    public void SetUpInitialBuildings()
    {
        var initialBuild = GetChildren();
        foreach (var node in initialBuild)
        {
            if(node is BuildingBase building)
            {
                _tmpBuilding = building;
                _selectedBuildingId = Regex.Replace(building.Name, @"\d+$", "");
                _tmpBuilding.Name = "tmpBuilding";
                TrySpawnBuilding(false);
                _tmpBuilding.Name = "tobedeleted";
                _tmpBuilding.QueueFree();
                _tmpBuilding = null;
            }
        }
    }

    public void DestroyBuilding(string buildingName)
    {
        GD.Print($"尝试销毁建筑 {buildingName}");
        var building = GetNode<BuildingBase>(buildingName);
        GridMgr.Instance.UpdateOccupation(building, true);
        SpawnedBuildings[building.Id].Remove(building);
        var neighbors = GridMgr.Instance.GetNeighbors(building);
        foreach (var name in neighbors)        
            GetNode<BuildingBase>(name).RecomputeAdjBonus();
        building.QueueFree();
        EmitSignalDestroyedBuilding();
    }
    
    public void UpgradeBuilding(string buildingName)
    {
        var id = buildingName.Split("_")[0];
        var newId = BuildingDatabase.GetById(id).UpgradeTo;
        if (string.IsNullOrEmpty(newId))
            return;
        ModifyAvailableBuildingList(true, newId);
        DestroyBuilding(buildingName);
        _upgrading = true;
        SelectBuilding(newId);
    }
    
    public void ModifyAvailableBuildingList(bool add, string buildingId)
    {
        if(add)
        {
            if(AvailableBuildingIds.Contains(buildingId))
                return;
            AvailableBuildingIds.Add(buildingId);
        }
        else
        {
            AvailableBuildingIds.Remove(buildingId);
        }
        EmitSignalAvailableBuildingIdsChanged(add, buildingId);
    }
}

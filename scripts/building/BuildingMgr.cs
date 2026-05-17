using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;


public partial class BuildingMgr : Node2D
{
    public static BuildingMgr Instance { get; private set; }
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
            if(_upgradedBuilding != null)
            {
                //if cancel upgrade, put back the original building
                GD.Print("Respawning original building after canceling upgrade");
                SpawnBuilding(_upgradedBuilding);
                _upgradedBuilding = null;
            }
            EmitSignalExitEditorMode();
        }
    }
    public class BuildingBuildData
    {
        public string Id;
        public Vector2 LocalPos;
        public int ZIdx;
        public int RotationStep;
    }
    private BuildingBuildData _upgradedBuilding  = null;

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
        if (_tmpBuilding != null) _tmpBuilding.Name = "tmpBuilding_toBeFreed";
        _tmpBuilding?.QueueFree();
        var buildingScene = GD.Load<PackedScene>(BuildingDatabase.GetById(buildingId).ScenePath);
        _tmpBuilding = buildingScene.Instantiate<BuildingBase>();
        _tmpBuilding.Name = "tmpBuilding";
        _tmpBuilding.Id = buildingId;
        _tmpBuilding.Modulate = _tmpBuilding.Modulate with { A = 0.5f };
        AddChild(_tmpBuilding);
        _tmpBuilding.GlobalPosition = GetLocalMousePosition();
    }

    public void TrySpawnBuilding(bool cost=true)
    {
        if (_tmpBuilding == null)
            return;

        var offset = GridMgr.Instance.CheckSpace(_tmpBuilding, out var zIdx);
        if (!offset.HasValue)
            return;

        var buildingData = BuildingDatabase.GetById(_tmpBuilding.Id);
        if (buildingData == null)
            return;

        float actualCost;
        if(_upgradedBuilding != null)
        {
            var originalData = BuildingDatabase.GetById(_upgradedBuilding.Id);
            if (!originalData.UpgradeCost.TryGetValue(buildingData.Id, out var upgradeCost))
            {
                GD.PrintErr($"未找到建筑升级配置: {originalData.Id} -> {buildingData.Id}");
                return;
            }
            actualCost = upgradeCost;
        }
        else
        {
            actualCost = buildingData.Cost;
        }
        if (cost && !MoneyMgr.Instance.TrySpend(actualCost))
        {
            WarningPanel.Instance.ShowWarning($"钱不够，需要 ￥{buildingData.Cost}");
            return;
        }
        var buildData = new BuildingBuildData
        {
            Id = _tmpBuilding.Id,
            LocalPos = offset.Value + _tmpBuilding.Position,
            ZIdx = zIdx,
            RotationStep = _tmpBuilding.RotationStep
        };

        SpawnBuilding(buildData);
    }

    public void SpawnBuilding(BuildingBuildData buildData)
    {
        var buildingData = BuildingDatabase.GetById(buildData.Id);
        if (buildingData == null) 
            return;
        //实例化
        var buildingScene = GD.Load<PackedScene>(buildingData.ScenePath);
        var building = buildingScene.Instantiate<BuildingBase>();
        AddChild(building);
        building.Name = buildingData.Id + '_' + _spawningCnt++.ToString();
        building.ZIndex = buildData.ZIdx;
        building.Position = buildData.LocalPos;
        building.Id = buildingData.Id;
        building.RotationStep = buildData.RotationStep;
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
        if (_upgradedBuilding != null)
        {
            _upgradedBuilding = null;
            IsEditing = false;
        }
    }

    public void SetUpInitialBuildings()
    {
        var initialBuild = GetChildren();
        foreach (var node in initialBuild)
        {
            if (node is not BuildingBase building) continue;
            
            if(building.Name.ToString().StartsWith("region_"))
            {
                GridMgr.Instance.RegisterRegion(building);
                continue;
            }
            
            _tmpBuilding = building;
            _tmpBuilding.Id = Regex.Replace(building.Name, @"_\d+$", "");
            _tmpBuilding.Name = "tmpBuilding";
            TrySpawnBuilding(false);
            _tmpBuilding.Name = "tobedeleted";
            _tmpBuilding.QueueFree();
            _tmpBuilding = null;
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
    
    public void TryUpgradeBuilding(string buildingName, string newId)
    {
        var building = GetNode<BuildingBase>(buildingName);
        var id = buildingName.Split("_")[0];
        if (string.IsNullOrEmpty(newId))
            return;
        _upgradedBuilding = new BuildingBuildData
        {
            Id = id,
            LocalPos = building.Position,
            ZIdx = building.ZIndex
        };
        ModifyAvailableBuildingList(true, newId);
        DestroyBuilding(buildingName);
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

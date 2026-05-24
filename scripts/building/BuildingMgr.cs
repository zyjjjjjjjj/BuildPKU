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
    public List<string> AvailableUpgradingIds { get; private set; } = [];

    [Signal]
    public delegate void SpawnedBuildingEventHandler(BuildingBase building);
    [Signal]
    public delegate void DestroyedBuildingEventHandler();
    [Signal]
    public delegate void EnterEditorModeEventHandler();
    [Signal]
    public delegate void ExitEditorModeEventHandler();
    [Signal]
    public delegate void AvailableBuildingChangedEventHandler(bool added, string buildingId);
    [Signal]
    public delegate void ActiveBuildingChangedEventHandler(string buildingId);
    
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
        AvailableBuildingIds = [..BuildingDatabase.GetInitiallyAvailableIds()];
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

        // switch (keyEvent.Keycode)
        // {
        //     case Key.Q:
        //         if (_tmpBuilding == null)
        //             break;
        //         _tmpBuilding.RotateBuilding(false);
        //         AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildRotate);
        //         break;
        //     case Key.E:
        //         if (_tmpBuilding == null)
        //             break;
        //         _tmpBuilding.RotateBuilding(true);
        //         AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildRotate);
        //         break;
        // }
    }

    private void SelectBuilding(string buildingId)
    {
        SelectBuilding(buildingId, true);
    }

    private void SelectBuilding(string buildingId, bool playSfx)
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
        BuildingSideBar.Instance.UpdateActiveBuilding(buildingId);
        if (playSfx)
            AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildSelect);
    }

    public void TrySpawnBuilding(bool cost=true, bool exitEditingWhenLimitReached=true)
    {
        if (_tmpBuilding == null)
            return;

        var offset = GridMgr.Instance.CheckSpace(_tmpBuilding, out var zIdx);
        if (!offset.HasValue)
        {
            if (cost)
                AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildInvalid);
            return;
        }

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
            actualCost = buildingData.ActualCost;
        }
        if (cost && !MoneyMgr.Instance.TrySpend(actualCost))
        {
            WarningPanel.Instance.ShowWarning($"钱不够，需要 ￥{buildingData.ActualCost}");
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
        if (cost)
            AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildPlace);
        if (!AvailableBuildingIds.Contains(_tmpBuilding.Id))
        {
            IsEditing = false;
            return;
        }
        if (_upgradedBuilding != null)
        {
            _upgradedBuilding = null;
            IsEditing = false;
        }
        return;
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
        building.RecomputeProps();
        
        //更新信息
        if (SpawnedBuildings.TryGetValue(buildingData.Id, out var list))
        {
            list.Add(building);
        }
        else
            SpawnedBuildings[buildingData.Id] = [building];
        
        UpdateAvailableBuildingList(buildingData.Id);
        
        GridMgr.Instance.UpdateOccupation(building);
        
        //计算邻居相邻加成
        var neighbors = GridMgr.Instance.GetNeighbors(building);
        foreach (var name in neighbors)
            GetNode<BuildingBase>(name).RecomputeProps();
        
        EmitSignalSpawnedBuilding(building);
    }

    public void SetUpInitialBuildingsAndRegions()
    {
        var initialBuild = GetChildren();
        foreach (var node in initialBuild)
        {
            if (node is not BuildingBase building) continue;
            
            if(building.Name.ToString().StartsWith("region_"))
            {
                GridMgr.Instance.RegisterRegion(building);
                building.QueueFree();
                continue;
            }
            
            _tmpBuilding = building;
            _tmpBuilding.Id = Regex.Replace(building.Name, @"_\d+$", "");
            _tmpBuilding.Name = "tmpBuilding";
            TrySpawnBuilding(false, false);
            if (_tmpBuilding != null) 
                _tmpBuilding.Name = "tobedeleted";
            _tmpBuilding?.QueueFree();
            _tmpBuilding = null;
        }
    }

    public void DestroyBuilding(string buildingName)
    {
        DestroyBuilding(buildingName, true);
    }

    private void DestroyBuilding(string buildingName, bool playSfx)
    {
        GD.Print($"尝试销毁建筑 {buildingName}");
        var building = GetNode<BuildingBase>(buildingName);
        GridMgr.Instance.UpdateOccupation(building, true);
        SpawnedBuildings[building.Id].Remove(building);
        UpdateAvailableBuildingList(building.Id);
        var neighbors = GridMgr.Instance.GetNeighbors(building);
        foreach (var name in neighbors)        
            GetNode<BuildingBase>(name).RecomputeProps();
        building.QueueFree();
        if (playSfx)
            AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildRemove);
        EmitSignalDestroyedBuilding();
    }
    
    public void TryUpgradeBuilding(string buildingName, string newId)
    {
        var building = GetNode<BuildingBase>(buildingName);
        if (string.IsNullOrEmpty(newId))
            return;
        _upgradedBuilding = new BuildingBuildData
        {
            Id = building.Id,
            LocalPos = building.Position,
            ZIdx = building.ZIndex
        };
        AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.BuildUpgrade);
        DestroyBuilding(buildingName, false);
        SelectBuilding(newId, false);
    }
    
    public void ModifyAvailableBuildingList(bool add, string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return;

        if(add)
        {
            if(AvailableBuildingIds.Contains(buildingId))
                return;
            AvailableBuildingIds.Add(buildingId);
            AvailableUpgradingIds.Remove(buildingId);
        }
        else
        {
            AvailableBuildingIds.Remove(buildingId);
        }
        EmitSignalAvailableBuildingChanged(add, buildingId);
    }

    public void UpdateAvailableBuildingList(string buildingId)
    {
        var buildingData = BuildingDatabase.GetById(buildingId);
        if (buildingData == null)
            return;
        var currentCount = SpawnedBuildings.TryGetValue(buildingId, out var list) ? list.Count : 0;
        if (buildingData.Limit >= 0 && currentCount >= buildingData.Limit)
        {
            ModifyAvailableBuildingList(false, buildingId);
        }
        else
        {
            if (!AvailableBuildingIds.Contains(buildingId))
                ModifyAvailableBuildingList(true, buildingId);
        }
    }

    public List<BuildingBase> GetBuildingsById(string id)
    {
        SpawnedBuildings.TryGetValue(id, out var result);
        return result ?? [];
    }
    
    public List<BuildingBase> GetBuildingsByTag(string tag)
    {
        List<BuildingBase> result = [];
        foreach (var pair in SpawnedBuildings)
        {
            var buildingData = BuildingDatabase.GetById(pair.Key);
            if (buildingData.Tags.Contains(tag))
                result.AddRange(pair.Value);
        }
        return result;
    }
    
    public List<BuildingBase> GetBuildingsByIdOrTag(string idOrTag)
    {
        return BuildingDatabase.IsId(idOrTag) ? GetBuildingsById(idOrTag) : GetBuildingsByTag(idOrTag);
    }
    
    
    public void UnlockBuildings(string buildingId)
    {
        if (buildingId == null)
            return;
        if (BuildingDatabase.GetById(buildingId) == null)
            return;
        UpdateAvailableBuildingList(buildingId);
    }
    
    public void UnlockUpgradedBuildings(string buildingId)
    {
        if (buildingId == null)
            return;
        var buildingData = BuildingDatabase.GetById(buildingId);
        if (buildingData == null)
            return;
        AvailableUpgradingIds.Add(buildingId);
    }

    public List<string> BuildingNameToId(List<string> names)
    {
        var result = new List<string>();
        foreach (var name in names)
        {
            var building = GetNode<BuildingBase>(name);
            if (building != null)
                result.Add(building.Id);
        }
        return result;
    }
}

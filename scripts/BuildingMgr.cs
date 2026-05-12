using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

public class BuildingData
{
    public string Id{get;set;}
    public string Name{get;set;}
    public string IconPath => "res://icon.svg";
    public string ScenePath => $"res://buildings/{Id}.tscn";
    public float Research { get; set; }
    public float Education { get; set; }
    public float Environment { get; set; }
    public float Cost { get; set; }
    public float Benefit { get; set; }
    public float Population { get; set; }
}

public partial class BuildingMgr : Node2D
{
    public static BuildingMgr Instance { get; private set; }
    private string _selectedBuildingId = string.Empty;
    private BuildingBase _tmpBuilding;
    public readonly List<BuildingBase> SpawnedBuildings = [];
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
    public Dictionary<string, BuildingData> AllBuildingDict => _allBuildingDict;
    private Dictionary<string, BuildingData> _allBuildingDict;
    private string _buildingListJsonPath = "res://buildings/BuildingsID.json";

    [Signal]
    public delegate void SpawnedBuildingEventHandler(BuildingBase building);
    [Signal]
    public delegate void EnterEditorModeEventHandler();
    [Signal]
    public delegate void ExitEditorModeEventHandler();
    
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
        _allBuildingDict = BuildingDatabase.GetAll();
    }

    public override void _Process(double delta)
    {
        if (_tmpBuilding == null)
            return;
        _tmpBuilding.Position = GetLocalMousePosition();
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && IsEditing)
            {
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
        var buildingScene = GD.Load<PackedScene>(AllBuildingDict[buildingId].ScenePath);
        _tmpBuilding = buildingScene.Instantiate<BuildingBase>();
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

        var buildingData = AllBuildingDict[_selectedBuildingId];
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
        var buildingScene = GD.Load<PackedScene>(AllBuildingDict[_selectedBuildingId].ScenePath);
        var building = buildingScene.Instantiate<BuildingBase>();
        AddChild(building);
        SpawnedBuildings.Add(building);
        building.Name = _selectedBuildingId + '_' + SpawnedBuildings.Count.ToString();
        building.Position = localPos;
        building.RotationStep = _tmpBuilding.RotationStep;
        building.ZIndex = zIdx;
        building.InitializeProperties(AllBuildingDict[_selectedBuildingId]);
        EmitSignalSpawnedBuilding(building);
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
}

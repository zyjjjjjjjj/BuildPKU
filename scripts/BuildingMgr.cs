using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingMgr : Node2D
{
    private string _selectedBuildingName = string.Empty;
    private string _selectedBuildingId = string.Empty;
    private GridMgr _gridMgr;
    private BuildingBase _tmpBuilding;
    private List<BuildingBase> _spawnedBuildings = new List<BuildingBase>();
    [Signal]
    public delegate void SpawnedBuildingEventHandler(BuildingBase building);
    public override void _Ready()
    {
        _gridMgr = GetTree().CurrentScene.GetNode<GridMgr>("gridMgr");
        if (_gridMgr == null)
        {
            GD.PrintErr("未找到 gridMgr 节点");
        }
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
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
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
            case Key.Key1:
                SelectBuilding("dorm", "宿舍");
                break;

            case Key.Key2:
                SelectBuilding("building", "实验室");
                break;

            case Key.Key3:
                SelectBuilding("green", "绿地");
                break;
            
            case Key.Escape:
                _selectedBuildingName = string.Empty;
                _tmpBuilding?.QueueFree();
                _tmpBuilding = null;
                _gridMgr.ShowGrid = false;
                GD.Print("取消选择");
                break;
            
            case Key.Q:
                _tmpBuilding?.RotateBuilding(false);
                break;
            case Key.E:
                _tmpBuilding?.RotateBuilding(true);
                break;
        }
    }

    private void SelectBuilding(string buildingId, string displayName)
    {
        GD.Print($"选择：{displayName}");
        _selectedBuildingName = displayName;
        _selectedBuildingId = buildingId;
        if (_tmpBuilding != null) _tmpBuilding.Name = "tmpBuilding_toBeFreed";
        _tmpBuilding?.QueueFree();
        var buildingScene = GD.Load<PackedScene>($"res://buildings/{buildingId}.tscn");
        _tmpBuilding = buildingScene.Instantiate<BuildingBase>();
        _tmpBuilding.Name = "tmpBuilding";
        _tmpBuilding.Modulate = _tmpBuilding.Modulate with { A = 0.5f };
        AddChild(_tmpBuilding);
        _gridMgr.ShowGrid = true;
    }

    public void TrySpawnBuilding()
    {
        var offset = _gridMgr.CheckSpace(_tmpBuilding, out var zIdx);
        if(offset.HasValue)
            SpawnBuilding(offset.Value + _tmpBuilding.Position, zIdx);
    }

    public void SpawnBuilding(Vector2 localPos, int zIdx)
    {
        var buildingScene = GD.Load<PackedScene>($"res://buildings/{_selectedBuildingId}.tscn");
        var building = buildingScene.Instantiate<BuildingBase>();
        AddChild(building);
        _spawnedBuildings.Add(building);
        building.Name = _spawnedBuildings.Count.ToString() + '_' + _selectedBuildingName;
        building.Position = localPos;
        building.RotationStep = _tmpBuilding.RotationStep;
        building.ZIndex = zIdx;
        EmitSignalSpawnedBuilding(building);
    }
}

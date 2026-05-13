using Godot;
using System;

public partial class UIMgr : CanvasLayer
{
	[Export] private BuildingInfoPanel _buildingInfoPanel;
	public static UIMgr Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if(Instance == this)
			Instance = null;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void ShowBuildingInfo(BuildingBase building)
	{
		_buildingInfoPanel.SetBuilding(building);
		_buildingInfoPanel.Visible = true;
	}
	
	public void HideBuildingInfo()
	{
		_buildingInfoPanel.Visible = false;
	}
}

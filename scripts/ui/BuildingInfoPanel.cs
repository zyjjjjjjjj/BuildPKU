using Godot;
using System;

public partial class BuildingInfoPanel : PanelContainer
{
	[Export] private Label _idLabel;
	[Export] private Label _resLabel;
	[Export] private Label _eduLabel;
	[Export] private Label _envLabel;
	[Export] private Label _popLabel;
	[Export] private Label _benLabel;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!Visible) return;
		var pos = GetViewport().GetMousePosition() - Size;
		if (pos.X < 0) pos.X += (Size.X + 20);
		if (pos.Y < 0) pos.Y += (Size.Y + 20);
		GlobalPosition = pos;
	}

	public void SetBuilding(BuildingBase building)
	{
		_idLabel.Text = building.buildingName;
		_resLabel.Text = $"{building.Research:F2}";
		_eduLabel.Text = $"{building.Education:F2}";
		_envLabel.Text = $"{building.Environment:F2}";
		_popLabel.Text = $"{building.Population:F2}";
		_benLabel.Text = $"{building.Benefit:F2}";
		return;
	}
}

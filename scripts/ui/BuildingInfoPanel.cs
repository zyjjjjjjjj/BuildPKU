using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingInfoPanel : PanelContainer
{
	[Export] private Label _idLabel;
	[Export] private Label _resLabel;
	[Export] private Label _eduLabel;
	[Export] private Label _envLabel;
	[Export] private Label _popLabel;
	[Export] private Label _benLabel;
	[Export] private Label _buffLabel;
	[Export] private Button _destroyButton;
	[Export] private Button _upgradeButton;

	private string _instanceName;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_destroyButton.Pressed += () => {
			BuildingMgr.Instance.DestroyBuilding(_instanceName);
			Visible = false;
		};
		_upgradeButton.Pressed += () => {
			BuildingMgr.Instance.UpgradeBuilding(_instanceName);
			Visible = false;
		};
		Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!Visible) return;
	}

	public void Show(BuildingBase building)
	{
		_instanceName = building.Name;
		_idLabel.Text = building.BuildingName;
		_resLabel.Text = $"{building.Research:F2}";
		_eduLabel.Text = $"{building.Education:F2}";
		_envLabel.Text = $"{building.Environment:F2}";
		_popLabel.Text = $"{building.Population:F2}";
		_benLabel.Text = $"{building.Benefit:F2}";
		
		ShowRelatedBuffs(building);

		if (string.IsNullOrEmpty(building.UpgradeTo))
		{
			_upgradeButton.Text = "无法升级";
			_upgradeButton.Disabled = true;
		}
		else
		{
			_upgradeButton.Text = $"花费 {building.UpgradeCost} 升级到 {BuildingDatabase.GetById(building.UpgradeTo).Name} ";
			_upgradeButton.Disabled = false;
		}
		
		if (building.Destroyable)
		{
			_destroyButton.Text = "拆除";
			_destroyButton.Disabled = false;
		}
		else
		{
			_destroyButton.Text = "不可拆除";
			_destroyButton.Disabled = true;
		}
		
		ResetSize();
		var pos = GetViewport().GetCanvasTransform() * building.GlobalPosition - Size;
		if (pos.X < 0) pos.X += (Size.X + 20);
		if (pos.Y < 0) pos.Y += (Size.Y + 20);
		GlobalPosition = pos;
		Visible = true;
		return;
	}
	
	public void ShowRelatedBuffs(BuildingBase building)
	{
		var relatedBuffs = building.RelatedBuffs;
		if(relatedBuffs.Count == 0)
		{
			_buffLabel.Visible = false;
			return;
		}
		List<string> lines= new List<string>();
		foreach (var buff in relatedBuffs)
		{
			int remainingYears = buff.GetRemainingYears(TimeMgr.Instance.CurrentYear);
			string duration;
			switch (remainingYears)
			{
				case < 0:
					duration = "永久";
					break;
				case 0:
					duration = "已结束";
					break;
				default:
					duration = $"剩余 {remainingYears} 年";
					break;
			}
			lines.Add($"{buff.Data.Description} ({duration})");
		}
		var text = string.Join("\n", lines);
		_buffLabel.Text = text;
		_buffLabel.Visible = true;
		return;
	}
}

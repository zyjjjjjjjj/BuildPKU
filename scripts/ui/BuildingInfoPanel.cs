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
	[Export] private MarginContainer _buffPanel;
	[Export] private Label _buffLabel;
	[Export] private Button _destroyButton;
	[Export] private MarginContainer _upgradePanel;
	[Export] private VBoxContainer _upgradeContainer;

	private string _instanceName;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_destroyButton.Pressed += () => {
			BuildingMgr.Instance.DestroyBuilding(_instanceName);
			HidePanel();
		};
		HidePanel();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!Visible) return;
	}

	public void ShowPanel(BuildingBase building)
	{
		_instanceName = building.Name;
		_idLabel.Text = building.DisplayName;
		_resLabel.Text = $"{building.Research:F2}";
		_eduLabel.Text = $"{building.Education:F2}";
		_envLabel.Text = $"{building.Environment:F2}";
		_popLabel.Text = $"{building.Population:F2}";
		_benLabel.Text = $"{building.Benefit:F2}";
		
		ShowRelatedBuffs(building);
		
		ShowUpgradeOptions(building);
		
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
		if (pos.X < 0) pos.X += (Size.X);
		if (pos.Y < 0) pos.Y += (Size.Y);
		GlobalPosition = pos;
		Visible = true;
		return;
	}
	
	public void ShowRelatedBuffs(BuildingBase building)
	{
		_buffLabel.Text = string.Empty;
		
		var relatedBuffs = building.RelatedBuffs;
		if(relatedBuffs.Count == 0)
		{
			_buffPanel.Visible = false;
			return;
		}
		List<string> lines= new List<string>();
		foreach (var buff in relatedBuffs)
			lines.Add($"{buff.Description}");
		var text = string.Join("\n", lines);
		_buffLabel.Text = text;
		_buffPanel.Visible = true;
		return;
	}
	
	public void ShowUpgradeOptions(BuildingBase building)
	{
		var children = _upgradeContainer.GetChildren();
		foreach (var child in children)
		{
			if (child is not Button button) continue;
			_upgradeContainer.RemoveChild(button);
			button.QueueFree();
		}
		
		var upgradeCost = BuildingDatabase.GetById(building.Id).UpgradeCost;
		if(upgradeCost.Count == 0)
		{
			_upgradePanel.Visible = false;
			return;
		}
		foreach (var kv in upgradeCost)
		{
			if(!BuildingMgr.Instance.AvailableUpgradingIds.Contains(kv.Key))
				continue;
			var button = new Button();
			var buildingData = BuildingDatabase.GetById(kv.Key);
			if(buildingData == null)
			{
				GD.PushWarning($"找不到升级选项的建筑配置: {kv.Key}");
				continue;
			}
			button.Text = $"{buildingData.Name}  费用: {kv.Value}";
			button.ThemeTypeVariation = "SuccessButton";
			_upgradeContainer.AddChild(button);
			button.Pressed += () =>
			{
				BuildingMgr.Instance.TryUpgradeBuilding(building.Name, kv.Key);
				HidePanel();
			};
		}
		_upgradePanel.Visible = true;
		return;
	}
	
	public void HidePanel()
	{
		Visible = false;
		var children = _upgradeContainer.GetChildren();
		foreach (var child in children)
		{
			if (child is not Button button) continue;
			button.QueueFree();
		}
		return;
	}
}

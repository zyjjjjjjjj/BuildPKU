using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingInfoPanel : PanelContainer
{
	public static BuildingInfoPanel Instance { get; private set; }
	private const float MaxPanelViewportRatio = 0.65f;
	private const float MinPanelMaxHeight = 260f;

	[Export] private ScrollContainer _scrollContainer;
	[Export] private Label _idLabel;
	[Export] private Label _resLabel;
	[Export] private Label _eduLabel;
	[Export] private Label _envLabel;
	[Export] private Label _popLabel;
	[Export] private Label _benLabel;
	[Export] private MarginContainer _buffPanel;
	[Export] private VBoxContainer _buffContainer;
	[Export] private Button _destroyButton;
	[Export] private MarginContainer _upgradePanel;
	[Export] private VBoxContainer _upgradeContainer;

	private string _instanceName = null;
	
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
		_destroyButton.Pressed += () => {
			BuildingMgr.Instance.DestroyBuilding(_instanceName);
			HidePanel();
		};
		_destroyButton.MouseEntered += () => AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.UiHover);
		HidePanel();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!Visible)
			return;
		if(!GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
			RefreshPanel();
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
		
		_scrollContainer.ScrollVertical = 0;
		ApplyMaxHeight();
		var pos = GetViewport().GetCanvasTransform() * building.GlobalPosition - Size;
		if (pos.X < 0) pos.X += (Size.X);
		if (pos.Y < 0) pos.Y += (Size.Y);
		var viewportSize = GetViewportRect().Size;
		pos.X = Mathf.Clamp(pos.X, 0, Mathf.Max(0, viewportSize.X - Size.X));
		pos.Y = Mathf.Clamp(pos.Y, 0, Mathf.Max(0, viewportSize.Y - Size.Y));
		GlobalPosition = pos;
		Visible = true;
		return;
	}

	private void ApplyMaxHeight()
	{
		_scrollContainer.CustomMinimumSize = Vector2.Zero;
		ResetSize();

		var maxHeight = Mathf.Max(MinPanelMaxHeight, GetViewportRect().Size.Y * MaxPanelViewportRatio);
		if (_scrollContainer.GetChildCount() == 0)
			return;

		var content = _scrollContainer.GetChild<Control>(0);
		var contentHeight = content.GetCombinedMinimumSize().Y;
		_scrollContainer.CustomMinimumSize = new Vector2(0, Mathf.Min(contentHeight, maxHeight));
		ResetSize();
	}
	
	public void ShowRelatedBuffs(BuildingBase building)
	{
		var children = _buffContainer.GetChildren();
		foreach (var child in children)		
		{
			if(child.Name == "Title") continue;
			_buffContainer.RemoveChild(child);
			child.QueueFree();
		}
		
		var relatedBuffs = building.RelatedBuffs;
		if(relatedBuffs.Count == 0)
		{
			_buffPanel.Visible = false;
			return;
		}
		
		HSeparator lastSeparator = null;
		foreach (var buff in relatedBuffs)
		{
			var label = new Label();
			label.Text = buff.Description;
			_buffContainer.AddChild(label);
			var separator = new HSeparator();
			_buffContainer.AddChild(separator);
			lastSeparator = separator;
		}
		_buffContainer.RemoveChild(lastSeparator);
		lastSeparator?.QueueFree();
		_buffPanel.Visible = true;
		return;
	}
	
	public void ShowUpgradeOptions(BuildingBase building)
	{
		_upgradePanel.Visible = false;
		var children = _upgradeContainer.GetChildren();
		foreach (var child in children)
		{
			if (child is not Button button) continue;
			_upgradeContainer.RemoveChild(button);
			button.QueueFree();
		}
		
		var upgradeCost = BuildingDatabase.GetById(building.Id).UpgradeCost;
		foreach (var kv in upgradeCost)
		{
			if(!BuildingMgr.Instance.AvailableUpgradingIds.Contains(kv.Key))
				continue;
			var currentCnt = BuildingMgr.Instance.SpawnedBuildings.TryGetValue(kv.Key, out var list) ? list.Count : 0;
			var limit = BuildingDatabase.GetById(kv.Key).Limit;
			if (limit >= 0 && currentCnt >= limit)
				continue;
			_upgradePanel.Visible = true;
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
			button.MouseEntered += () => AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.UiHover);
			button.Pressed += () =>
			{
				BuildingMgr.Instance.TryUpgradeBuilding(building.Name, kv.Key);
				HidePanel();
			};
		}
		return;
	}
	
	public void HidePanel()
	{
		Visible = false;
		_instanceName = null;
		var children = _upgradeContainer.GetChildren();
		foreach (var child in children)
		{
			if (child is not Button button) continue;
			button.QueueFree();
		}
		return;
	}
	
	public void RefreshPanel()
	{
		var buildingName = GridMgr.Instance.GetCurrentPointingBuildingName();
		if (buildingName == _instanceName)
			return;
		if(string.IsNullOrEmpty(buildingName))
		{
			HidePanel();
			return;
		}
		var building = BuildingMgr.Instance.GetNodeOrNull<BuildingBase>(buildingName);
		if (building == null)
		{
			HidePanel();
			return;
		}
		ShowPanel(building);
	}
}

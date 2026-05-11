using System.Collections.Generic;
using System.IO;
using Godot;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public partial class BuildingSideBar : PanelContainer
{
	[Export] private ItemList _itemList;
	
	[Signal] public delegate void SideBarBuildingSelectedEventHandler(string id);
	const string JsonPath = "res://buildings/BuildingsID.json";

	private Dictionary<string, BuildingData> _buildingList;
	private BuildingMgr _buildingMgr;

	private long _onSelectedIndex = -1;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_buildingMgr = GetTree().CurrentScene.GetNode<BuildingMgr>("buildingMgr");
		_buildingList = _buildingMgr.AllBuildingDict;
		InitializeItemList();
	}
	private void InitializeItemList()
	{
		_itemList = GetNode<ItemList>("VBoxContainer/ItemList");
		// 清空并填充
		_itemList.Clear();
		foreach (var pair in _buildingList)
		{
			var data = pair.Value;
			var displayText = data.Name;
			Texture2D icon = GD.Load<Texture2D>(data.IconPath);
			_itemList.AddItem(displayText, icon);
			_itemList.SetItemMetadata(_itemList.GetItemCount() - 1, data.Id);
		}
		_itemList.ItemSelected += _OnSideBarSelectedBuilding;
	}

	private void _OnSideBarSelectedBuilding(long index)
	{
		GD.Print(index, _onSelectedIndex);
		if (index == _onSelectedIndex)
		{
			GD.Print("OnSelectedIndex");
			_DeselectAll();
			_onSelectedIndex = -1;
			return;
		}
		_onSelectedIndex = index;
		EmitSignal(SignalName.SideBarBuildingSelected, _itemList.GetItemMetadata((int)index).ToString());
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Right)
			{
				_DeselectAll();
			}
		}
		
		if (@event is not InputEventKey keyEvent)
			return;

		if (!keyEvent.Pressed || keyEvent.Echo)
			return;

		switch (keyEvent.Keycode)
		{
			case Key.Escape:
				_DeselectAll();
				break;
			case Key.Key1:
				_itemList.Select(0);
				_OnSideBarSelectedBuilding(0);
				break;
			case Key.Key2:
				_itemList.Select(1);
				_OnSideBarSelectedBuilding(1);
				break;
			case Key.Key3:
				_itemList.Select(2);
				_OnSideBarSelectedBuilding(2);
				break;
		}
	}

	private void _DeselectAll()
	{
		_onSelectedIndex = -1;
		_itemList.DeselectAll();
		EmitSignal(SignalName.SideBarBuildingSelected, string.Empty);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
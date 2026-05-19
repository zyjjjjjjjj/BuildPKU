using System;
using Godot;

public partial class BuildingSideBar : PanelContainer
{
	public static BuildingSideBar Instance { get; private set; }
	[Export] private ItemList _itemList;
	
	[Signal] public delegate void SideBarBuildingSelectedEventHandler(string id);

	private long _onSelectedIndex = -1;
	
	
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
		MouseFilter = MouseFilterEnum.Stop;
		InitializeItemList();
		BuildingMgr.Instance.AvailableBuildingChanged += UpdateItemList;
		BuildingMgr.Instance.ActiveBuildingChanged += UpdateActiveBuilding;
	}

	private void InitializeItemList()
	{
		_itemList = GetNode<ItemList>("VBoxContainer/ItemList");
		_itemList.MouseFilter = MouseFilterEnum.Stop;
		// 清空并填充
		_itemList.Clear();
		foreach (var id in BuildingDatabase.GetInitiallyAvailableIds())
		{
			var data = BuildingDatabase.GetById(id);
			var displayText = $"{data.Name}\n￥{data.ActualCost}";

			Texture2D icon = GD.Load<Texture2D>(data.IconPath);
			_itemList.AddItem(displayText, icon);
			_itemList.SetItemMetadata(_itemList.GetItemCount() - 1, data.Id);
		}
		_itemList.ItemSelected += _OnSideBarSelectedBuilding;
	}
	
	public void RefreshItemList()
	{
		_itemList.Clear();
		foreach (var id in BuildingMgr.Instance.AvailableBuildingIds)
		{
			var data = BuildingDatabase.GetById(id);
			var displayText = $"{data.Name}\n￥{data.ActualCost}";

			Texture2D icon = GD.Load<Texture2D>(data.IconPath);
			_itemList.AddItem(displayText, icon);
			_itemList.SetItemMetadata(_itemList.GetItemCount() - 1, data.Id);
		}
	}

	private void UpdateItemList(bool add, string buildingId)
	{
		if(add)
		{
			var data = BuildingDatabase.GetById(buildingId);
			var displayText = $"{data.Name}\n￥{data.ActualCost}";

			Texture2D icon = GD.Load<Texture2D>(data.IconPath);
			_itemList.AddItem(displayText, icon);
			_itemList.SetItemMetadata(_itemList.GetItemCount() - 1, data.Id);
		}
		else
		{
			for (var i = 0; i < _itemList.GetItemCount(); i++)
			{
				if (_itemList.GetItemMetadata(i).ToString() != buildingId) continue;
				_itemList.RemoveItem(i);
				break;
			}
		}
		return;
	}

	public override void _GuiInput(InputEvent @event)
	{
		AcceptWheelEvent(@event);
	}

	private void AcceptWheelEvent(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (!mouseEvent.Pressed)
			return;

		if (mouseEvent.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight)
			AcceptEvent();
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

	private void UpdateActiveBuilding(string buildingId)
	{
		_itemList.DeselectAll();
		_onSelectedIndex = -1;

		if (string.IsNullOrEmpty(buildingId))
			return;

		for (var i = 0; i < _itemList.GetItemCount(); i++)
		{
			if (_itemList.GetItemMetadata(i).ToString() != buildingId) continue;
			_onSelectedIndex = i;
			_itemList.Select(i);
			return;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

using Godot;
using System.Collections.Generic;

public partial class BuildingSideBar : PanelContainer
{
	public static BuildingSideBar Instance { get; private set; }
	private const int UpgradeTooltipLineLength = 16;
	[Export] private ItemList _itemList;
	[Export] private Color _itemBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
	
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
		BuildingMgr.Instance.SpawnedBuilding += building => UpdateItemList(building.Id);
		BuildingMgr.Instance.DestroyedBuilding += UpdateItemList;
		BuildingMgr.Instance.AvailableBuildingChanged += (add, id) => AddBuildingItem(id);
		BuildingMgr.Instance.ExitEditorMode += () => UpdateActiveBuilding(string.Empty);
	}

	private void InitializeItemList()
	{
		_itemList = GetNode<ItemList>("VBoxContainer/ItemList");
		_itemList.MouseFilter = MouseFilterEnum.Stop;
		_itemList.Clear();
		foreach (var id in BuildingDatabase.GetInitiallyAvailableIds())
			AddBuildingItem(id);
		_itemList.ItemSelected += _OnSideBarSelectedBuilding;
		_itemList.MouseEntered += () => AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.UiHover);
	}

	private void UpdateItem(int idx)
	{
		var buildingId = _itemList.GetItemMetadata(idx).ToString();
		var buildingData = BuildingDatabase.GetById(buildingId);
		var limit = buildingData.Limit;
		var available = limit < 0 || 
		                !BuildingMgr.Instance.SpawnedBuildings.TryGetValue(buildingId, out var list) ||
		                list.Count < limit;
		var text = $"{buildingData.Name}\n";
		text += available ? $"￥{buildingData.ActualCost}" : "已达上限";
		_itemList.SetItemText(idx, text);
		_itemList.SetItemDisabled(idx, !available);
	}
	
	public void RefreshItemList()
	{
		for (var i = 0; i < _itemList.GetItemCount(); i++)
		{
			UpdateItem(i);
		}
	}

	private void UpdateItemList(string buildingId)
	{
		for (var i = 0; i < _itemList.GetItemCount(); i++)
		{
			if (_itemList.GetItemMetadata(i).ToString() != buildingId) continue;
			UpdateItem(i);
			break;
		}
		return;
	}

	private void AddBuildingItem(string id)
	{
		var data = BuildingDatabase.GetById(id);
		if (data == null)
			return;
		var displayText = $"{data.Name}";
		Texture2D icon = CreateIconWithBackground(
			GD.Load<Texture2D>(data.IconPath),
			_itemBgColor,
			64
		);
		_itemList.AddItem(displayText, icon);
		var index = _itemList.GetItemCount() - 1;
		_itemList.SetItemMetadata(index, data.Id);
		_itemList.SetItemTooltip(index, BuildBuildingTooltip(data));
		UpdateItem(index);
	}
	
	private Texture2D CreateIconWithBackground(Texture2D source, Color bgColor, int size = 64)
	{
		if (source == null)
			return null;

		var bgImage = Image.Create(size, size, false, Image.Format.Rgba8);
		bgImage.Fill(bgColor);

		var srcImage = source.GetImage();
		srcImage.Convert(Image.Format.Rgba8);

		int padding = 8;
		int iconSize = size - padding * 2;
		srcImage.Resize(iconSize, iconSize, Image.Interpolation.Lanczos);

		var dstPos = new Vector2I(padding, padding);
		bgImage.BlendRect(srcImage, new Rect2I(Vector2I.Zero, srcImage.GetSize()), dstPos);

		return ImageTexture.CreateFromImage(bgImage);
	}

	private string BuildBuildingTooltip(BuildingData data)
	{
		var limitText = data.Limit > 0 ? data.Limit.ToString() : "无限";
		var upgradeText = data.UpgradeCost.Count > 0 ? string.Join(" / ", data.UpgradeCost.Keys) : "无";

		if (data.UpgradeCost.Count > 0)
		{
			var upgradeNames = new List<string>();
			foreach (var id in data.UpgradeCost.Keys)
			{
				var upgradeData = BuildingDatabase.GetById(id);
				upgradeNames.Add(upgradeData?.Name ?? id);
			}
			upgradeText = WrapUpgradeNames(upgradeNames, UpgradeTooltipLineLength, 5);
		}
		
		return $"{data.Name}\n" +
		       $"费用: {data.ActualCost}\n" +
		       $"科研: {data.Research}  教学: {data.Education}\n" +
		       $"环境: {data.Environment}  人口: {data.Population}\n" +
		       $"收益: {data.Benefit}\n" +
		       $"建造上限: {limitText}\n" +
		       $"可升级为: {upgradeText}";
	}

	private static string WrapUpgradeNames(List<string> upgradeNames, int maxLineLength, int prefixLength)
	{
		if (upgradeNames.Count == 0)
			return string.Empty;

		var lines = new List<string>();
		var currentLine = string.Empty;
		var firstLineLimit = Mathf.Max(1, maxLineLength - prefixLength);
		var continuationPrefix = new string(' ', prefixLength);

		foreach (var name in upgradeNames)
		{
			var nextLine = string.IsNullOrEmpty(currentLine)
				? name
				: $"{currentLine} / {name}";
			var currentLimit = lines.Count == 0 ? firstLineLimit : maxLineLength;

			if (currentLine.Length > 0 && nextLine.Length > currentLimit)
			{
				lines.Add(currentLine);
				currentLine = name;
				continue;
			}

			currentLine = nextLine;
		}

		if (!string.IsNullOrEmpty(currentLine))
			lines.Add(currentLine);

		for (var i = 1; i < lines.Count; i++)
			lines[i] = continuationPrefix + lines[i];

		return string.Join("\n", lines);
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
		var hadSelection = _onSelectedIndex >= 0;
		_onSelectedIndex = -1;
		_itemList.DeselectAll();
		EmitSignal(SignalName.SideBarBuildingSelected, string.Empty);
		if (hadSelection)
			AudioMgr.Instance?.PlaySfx(AudioMgr.Sfx.UiCancel);
	}

	public void UpdateActiveBuilding(string buildingId)
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

using Godot;

public partial class TimeMgr : Node
{
	public static TimeMgr Instance { get; private set; }

	private int _currentYear;
	public int CurrentYear
	{
		get
		{
			return _currentYear;
		}
		private set
		{
			_currentYear = value;
			EmitSignalYearAdvanced(_currentYear);
		}
	}
	public int EndYear = 2026;
	public int InitialYear = 1900;
	[Signal] public delegate void YearAdvancedEventHandler(int newYear);
	
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
		_currentYear = InitialYear;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void AdvanceYear()
	{
		if (CurrentYear < EndYear)
		{
			CurrentYear++;
			GD.Print(CurrentYear);
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent)
			return;

		if (!keyEvent.Pressed || keyEvent.Echo)
			return;

		switch (keyEvent.Keycode)
		{
			case Key.N:
				AdvanceYear();
				break;
		}
	}
}

using Godot;
using System;

public partial class GameMgr : Node2D
{
    public static GameMgr Instance { get; private set; }

    [Signal]
    public delegate void StartedNewTurnEventHandler();

    public override void _EnterTree()
    {
	    Instance = this;
    }

    public override void _ExitTree()
    {
	    if (Instance == this)
		    Instance = null;
    }

    // Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//set up initial building
		BuildingMgr.Instance.SetUpInitialBuildingsAndRegions();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void RestartGame()
	{
		GD.Print("restart");
		GetTree().ReloadCurrentScene();
	}

	public void StartNewTurn()
	{
		EmitSignalStartedNewTurn();
	}
}

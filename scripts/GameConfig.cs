using Godot;

public partial class GameConfig : Node
{
    public static GameConfig Instance { get; private set; }

    [Export] public int CellSize = 64;

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }
}
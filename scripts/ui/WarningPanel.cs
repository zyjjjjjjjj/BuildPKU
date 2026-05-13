using Godot;

public partial class WarningPanel : PanelContainer
{
    public static WarningPanel Instance { get; private set; }

    private Label _warningLabel;
    private Timer _warningTimer;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if(Instance == this)
            Instance = null;
    }
    
    public override void _Ready()
    {
        _warningLabel = GetNode<Label>("WarningLabel");
        _warningTimer = new Timer();
        _warningTimer.OneShot = true;
        _warningTimer.WaitTime = 1.5;
        _warningTimer.Timeout += ClearWarning;
        AddChild(_warningTimer);
    }

    public void ShowWarning(string message)
    {
        _warningLabel.Text = message;
        _warningLabel.Visible = true;
        _warningTimer.Start();
    }

    private void ClearWarning()
    {
        _warningLabel.Visible = false;
    }
}
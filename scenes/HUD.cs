using Godot;

public partial class HUD : Control
{
    public static HUD Instance { get; private set; }

    private Label _buildWarningLabel;
    private Timer _warningTimer;

    public override void _Ready()
    {
        Instance = this;

        _buildWarningLabel = GetNode<Label>("BuildWarningLabel");

        _warningTimer = new Timer();
        _warningTimer.OneShot = true;
        _warningTimer.WaitTime = 1.5;
        _warningTimer.Timeout += ClearWarning;
        AddChild(_warningTimer);
    }

    public void ShowBuildWarning(string message)
    {
        _buildWarningLabel.Text = message;
        _warningTimer.Start();
    }

    private void ClearWarning()
    {
        _buildWarningLabel.Text = "";
    }
}
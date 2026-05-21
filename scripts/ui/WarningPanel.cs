using Godot;

public partial class WarningPanel : PanelContainer
{
    public static WarningPanel Instance { get; private set; }

    private Label _warningLabel;
    private Timer _warningTimer;
    private Tween _warningTween;

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
        Visible = false;
    }

    public void ShowWarning(string message)
    {
        _warningTimer.Stop();
        _warningTween?.Kill();

        _warningLabel.Text = message;
        Visible = true;
        Modulate = new Color(1f, 1f, 1f, 0f);
        Scale = new Vector2(0.96f, 0.96f);

        _warningTween = CreateTween();
        _warningTween.SetParallel(true);
        _warningTween.TweenProperty(this, "modulate:a", 1f, 0.12f);
        _warningTween.TweenProperty(this, "scale", Vector2.One, 0.12f);

        _warningTimer.Start();
    }

    private void ClearWarning()
    {
        _warningTween?.Kill();
        _warningTween = CreateTween();
        _warningTween.TweenProperty(this, "modulate:a", 0f, 0.18f);
        _warningTween.TweenCallback(Callable.From(() => Visible = false));
    }
}

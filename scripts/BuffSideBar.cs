using Godot;

public partial class BuffSideBar : PanelContainer
{
    public static BuffSideBar Instance { get; private set; }

    [Export] private VBoxContainer _buffList;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        BuffMgr.Instance.BuffsDecay += Refresh;
        BuffMgr.Instance.BuffsChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        foreach (var child in _buffList.GetChildren())
            child.QueueFree();

        foreach (var buff in BuffMgr.Instance.ActiveWeightBuffs)
        {
            var label = new Label();

            int remainingYears = buff.GetRemainingYears(TimeMgr.Instance.CurrentYear);
            string duration = remainingYears < 0 ? "永久" : $"剩余 {remainingYears} 年";

            label.Text = $"{buff.Data.Name} ({duration})";
            label.TooltipText = buff.Data.Description;

            _buffList.AddChild(label);
        }
    }
}
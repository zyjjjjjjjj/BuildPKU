using Godot;
using System;
using System.Collections.Generic;

public partial class AudioMgr : Node
{
    public static AudioMgr Instance { get; private set; }

    public static class Bgm
    {
        public const string MainTheme = "main_theme";
        public const string GameOver = "game_over";
    }

    public static class Sfx
    {
        public const string UiClick = "ui_click";
        public const string UiHover = "ui_hover";
        public const string UiCancel = "ui_cancel";
        public const string BuildSelect = "build_select";
        public const string BuildPlace = "build_place";
        public const string BuildInvalid = "build_invalid";
        public const string BuildRotate = "build_rotate";
        public const string BuildRemove = "build_remove";
        public const string BuildUpgrade = "build_upgrade";
        public const string CardShow = "card_show";
        public const string CardChoose = "card_choose";
        public const string CardPanelExpand = "card_panel_expand";
        public const string CardPanelCollapse = "card_panel_collapse";
        public const string CameraZoom = "camera_zoom";
        public const string YearAdvance = "year_advance";
        public const string Warning = "warning";
    }

    [Export] private string _sfxBus = "Master";
    [Export(PropertyHint.Range, "-40,6,0.1")] private float _sfxVolumeDb = 0f;
    [Export] private string _bgmBus = "Master";
    [Export(PropertyHint.Range, "-40,6,0.1")] private float _bgmVolumeDb = -4f;

    private AudioStreamPlayer _bgmPlayer;
    private string _currentBgmId = string.Empty;
    private readonly Dictionary<string, AudioStream> _streams = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AudioStream> _bgmStreams = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _bgmPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        { Bgm.MainTheme, "res://assets/audio/bgm/HappyClappyLoop.ogg" },
        { Bgm.GameOver, "res://assets/audio/bgm/Game Over.ogg" },
    };
    private readonly Dictionary<string, string> _sfxPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        { Sfx.UiClick, "res://assets/audio/sfx/Audio/click1.ogg" },
        //{ Sfx.UiHover, "res://assets/audio/sfx/Audio/rollover1.ogg" },
        { Sfx.UiCancel, "res://assets/audio/sfx/Audio/mouserelease1.ogg" },
        { Sfx.BuildSelect, "res://assets/audio/sfx/Audio/switch1.ogg" },
        { Sfx.BuildPlace, "res://assets/audio/sfx/inventory_sound_effects/sell_buy_item.wav" },
        { Sfx.BuildInvalid, "res://assets/audio/sfx/inventory_sound_effects/metal-clash.wav" },
        { Sfx.BuildRotate, "res://assets/audio/sfx/Audio/switch9.ogg" },
        { Sfx.BuildRemove, "res://assets/audio/sfx/inventory_sound_effects/cloth-inventory.wav" },
        { Sfx.BuildUpgrade, "res://assets/audio/sfx/inventory_sound_effects/ring_inventory.wav" },
        { Sfx.CardShow, "res://assets/audio/sfx/inventory_sound_effects/turn_page.wav" },
        { Sfx.CardChoose, "res://assets/audio/sfx/inventory_sound_effects/ring_inventory.wav" },
        { Sfx.CardPanelExpand, "res://assets/audio/sfx/Audio/switch6.ogg" },
        { Sfx.CardPanelCollapse, "res://assets/audio/sfx/Audio/switch7.ogg" },
        //{ Sfx.CameraZoom, "res://assets/audio/sfx/Audio/switch1.ogg" },
        { Sfx.YearAdvance, "res://assets/audio/sfx/Audio/switch5.ogg" },
        { Sfx.Warning, "res://assets/audio/sfx/inventory_sound_effects/metal-clash.wav" },
    };

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
        _bgmPlayer = new AudioStreamPlayer
        {
            Bus = _bgmBus,
            VolumeDb = _bgmVolumeDb,
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(_bgmPlayer);

        LoadConfiguredBgm();
        LoadConfiguredSfx();
        CallDeferred(nameof(PlayBgm), Bgm.MainTheme);
    }

    public void PlaySfx(string id)
    {
        if (!_streams.TryGetValue(id, out var stream))
            return;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = _sfxBus,
            VolumeDb = _sfxVolumeDb,
        };

        AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    public void PlayBgm(string id)
    {
        if (_currentBgmId == id && _bgmPlayer.Playing)
            return;

        if (!_bgmStreams.TryGetValue(id, out var stream))
            return;

        _currentBgmId = id;
        _bgmPlayer.Stream = stream;
        _bgmPlayer.VolumeDb = _bgmVolumeDb;
        _bgmPlayer.Play();
        GD.Print($"Playing BGM: {id}");
    }

    public void StopBgm()
    {
        _currentBgmId = string.Empty;
        _bgmPlayer?.Stop();
    }

    private void LoadConfiguredBgm()
    {
        foreach (var (id, path) in _bgmPaths)
        {
            var stream = ResourceLoader.Load<AudioStream>(path);
            if (stream == null)
            {
                GD.PushWarning($"Failed to load BGM '{id}' from: {path}");
                continue;
            }

            if (stream is AudioStreamWav wav)
                wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;

            if (stream is AudioStreamOggVorbis ogg)
                ogg.Loop = id == Bgm.MainTheme;

            _bgmStreams[id] = stream;
        }
    }

    private void LoadConfiguredSfx()
    {
        foreach (var (id, path) in _sfxPaths)
        {
            var stream = ResourceLoader.Load<AudioStream>(path);
            if (stream == null)
            {
                GD.PushWarning($"Failed to load SFX '{id}' from: {path}");
                continue;
            }

            _streams[id] = stream;
        }
    }
}

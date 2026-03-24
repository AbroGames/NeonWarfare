using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.Root.Starters;
using Serilog;
using NodeContainer = NeonWarfare.Scenes.KludgeBox.NodeContainer;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.Root;

public partial class Root : Node2D
{
    [Logger] private ILogger _log;
    // values were picked empirically and need testing on other screen resolutions
    // TODO: KeyJ: test this on a resolution larger than my 1920x1080
    private readonly List<(int expectedWindowHeight, float scaleFactor)> _scaleFactorMappings =
    [
        (expectedWindowHeight: -1, scaleFactor: 0.75f), // smallest possible scale
        (expectedWindowHeight: 400, scaleFactor: 1.0f),
        (expectedWindowHeight: 700, scaleFactor: 1.15f),
        (expectedWindowHeight: 900, scaleFactor: 1.3f),
        (expectedWindowHeight: 1200, scaleFactor: 1.5f),
    ];
    private float _currentScale = 1;
    [Child] public NodeContainer MainSceneContainer { get; set; }
    [Child] public NodeContainer LoadingScreenContainer { get; set; }
    [Child] public RootPackedScenes PackedScenes { get; set; }

    private RootStarterManager _rootStarterManager;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        Callable.From(() => {
            Init();
            Start();
        }).CallDeferred();
    }

    private void Init()
    {
        RootData rootData = new RootData(MainSceneContainer, LoadingScreenContainer, PackedScenes, GetTree());
        _rootStarterManager = new RootStarterManager(rootData);
        _rootStarterManager.Init();
    }

    private void Start()
    {
        _rootStarterManager.Start();
    }
    

    public override void _Process(double delta)
    {
        UpdateStretchScale();
    }
    

    private float GetScaleForWindowSize(Vector2I size)
    {
        int currentWindowHeight = size.Y;
        return _scaleFactorMappings
            .Where(f => size.Y >= f.expectedWindowHeight)
            .Select(f => f.scaleFactor)
            .LastOrDefault(0.75f);
    }
    
    // called from the _Process method in the main file of this partial class
    private void UpdateStretchScale()
    {
        var window = GetTree().Root;
        var size = window.Size;
        float newScale = GetScaleForWindowSize(window.Size);

        if (!_currentScale.IsEqualApprox(newScale))
        {
            _log.Information("Adjusting scale for window size {sizeX}x{sizeY} to {newScale}", size.X, size.Y, newScale);
            _currentScale = newScale;
            GetTree().Root.ContentScaleFactor = newScale;
        }
    }
}

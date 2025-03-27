using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot
{
    // values were picked empirically and need testing on other screen resolutions
    // TODO: KeyJ: test this on a resolution larger than my 1920x1080
    private readonly List<(int expectedWindowHeight, float scaleFactor)> _scaleFactorMappings =
    [
        (expectedWindowHeight: -1, scaleFactor: 0.75f), // smallest possible scale
        (expectedWindowHeight: 400, scaleFactor: 1.0f),
        (expectedWindowHeight: 900, scaleFactor: 1.15f),
        (expectedWindowHeight: 1400, scaleFactor: 1.3f),
    ];
    
    private float _currentScale = 1;

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
        
        float newScale = GetScaleForWindowSize(window.Size);
        
        if (!_currentScale.IsEqualApprox(newScale))
        {
            Log.Debug($"Adjusting scale to {newScale}");
            _currentScale = newScale;
            GetTree().Root.ContentScaleFactor = newScale;
        }
    }
}
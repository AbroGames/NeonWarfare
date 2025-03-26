using Godot;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot
{
    private const int HeightThreshold0 = 400;
    private const int HeightThreshold1 = 900;
    private const int HeightThreshold2 = 1400;
    
    private const float Scale0 = 0.75f;
    private const float Scale1 = 1.0f;
    private const float Scale2 = 1.15f;
    private const float Scale3 = 1.3f;
    
    private float _currentScale = 1;
    
    private void UpdateStretchScale()
    {
        var viewport = GetTree().Root;
        int height = viewport.Size.Y;
    
        float newScale = Scale0;
    
        if (height >= HeightThreshold2)
        {
            newScale = Scale3;
        }
        else if (height >= HeightThreshold1)
        {
            newScale = Scale2;
        }
        else if (height >= HeightThreshold0)
        {
            newScale = Scale1;
        }

        if (_currentScale != newScale)
        {
            Log.Debug($"Adjusting scale to {newScale}");
            _currentScale = newScale;
            GetTree().Root.ContentScaleFactor = newScale;
        }
    }
}
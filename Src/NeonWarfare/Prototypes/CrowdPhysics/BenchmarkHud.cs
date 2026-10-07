using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>The read-only overlay: a CanvasLayer with one Label, updated by the root twice a second.</summary>
public partial class BenchmarkHud : CanvasLayer
{
    private Label _label;

    public override void _Ready()
    {
        _label = new Label
        {
            Position = new Vector2(12f, 12f),
        };
        _label.AddThemeFontSizeOverride("font_size", 15);
        AddChild(_label);
    }

    public void SetText(string text)
    {
        _label.Text = text;
    }
}

using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.PlayerSettingsInterface;

public partial class ColorRectExample : ColorRect
{
    [Export] [NotNull] public HSlider RedSlider { get; private set; }
    [Export] [NotNull] public HSlider GreenSlider { get; private set; }
    [Export] [NotNull] public HSlider BlueSlider { get; private set; }
    public override void _Ready()
    {
        Color = ClientRoot.Instance.PlayerSettings.PlayerColor;
    }

    public override void _Process(double delta)
    {
        NotNullChecker.CheckProperties(this);
        Color = new Color((float) RedSlider.Value/255f,
            (float) GreenSlider.Value/255f,
            (float) BlueSlider.Value/255f, 1);
        base._Process(delta);
    }
}

using Godot;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.PlayerSettingsInterface;

public partial class GreenSlider : HSlider
{
    public override void _Ready()
    {
        Value = ClientRoot.Instance.PlayerSettings.PlayerColor.G * 255f;
    }
}

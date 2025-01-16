using Godot;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.MainMenuInterface;

public partial class ExitButton : Button
{
    public override void _Ready()
    {
        Pressed += () => ClientRoot.Instance.Shutdown();
    }
}

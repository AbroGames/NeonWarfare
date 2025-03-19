using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;

namespace NeonWarfare.Scenes.Screen.SafeHud;

public partial class ToMenuButton : Button
{
    
    public override void _Ready()
    {
        Pressed += () =>
        {
            ClientRoot.Instance.CreateMainMenu();
        };
    }

}

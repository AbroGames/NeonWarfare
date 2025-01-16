using Godot;
using KludgeBox.Networking;

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

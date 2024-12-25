using Godot;
using KludgeBox.Networking;

namespace NeonWarfare;

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
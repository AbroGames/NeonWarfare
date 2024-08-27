using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;

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
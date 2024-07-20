using Godot;
using KludgeBox.Networking;

namespace NeonWarfare;

public partial class ToBattleButton : Button
{
    
    public override void _Ready()
    {
        Pressed += () =>
        {
            Netplay.SendToServer(new ClientWantToBattlePacket());
        };
    }

}
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

public partial class ToBattleButton : Button
{
    
    public override void _Ready()
    {
        Pressed += () =>
        {
            Network.SendPacketToServer(new ClientWantToBattlePacket());
        };
    }

}
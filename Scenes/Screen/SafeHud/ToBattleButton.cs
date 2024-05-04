using Godot;
using KludgeBox.Net;

namespace NeonWarfare;

public partial class ToBattleButton : Button
{
    
    public override void _Ready()
    {
        Pressed += () =>
        {
            NetworkOld.SendPacketToServer(new ClientWantToBattlePacket());
        };
    }

}
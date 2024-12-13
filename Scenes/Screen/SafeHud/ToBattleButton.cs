using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ToBattleButton : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            Network.SendToServer(new ServerGame.CS_WantToBattlePacket());
        };
    }

}
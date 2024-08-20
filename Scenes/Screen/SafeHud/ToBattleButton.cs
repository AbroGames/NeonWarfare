using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;

namespace NeonWarfare;

[GamePacket]
public class ToBattleButtonClickPacket : BinaryPacket;

public partial class ToBattleButton : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            Network.SendToServer(new ToBattleButtonClickPacket());
        };
    }

}
using System.Collections.Generic;
using Godot;
using KludgeBox.Networking;
using NeonWarfare;

namespace NeonWarfare.Scenes.Game.ClientGame.Ping;

public partial class PingChecker
{
    
    [GamePacket]
    public class SC_PingPacket(long pingId) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;

        public long PingId = pingId;
    }
}

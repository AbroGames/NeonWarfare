using System.Collections.Generic;
using Godot;
using KludgeBox.Networking;
using NeonWarfare;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
    
    [GamePacket]
    public class CS_WantToBattlePacket : BinaryPacket;
    
    [GamePacket]
    public class CS_PingPacket(long pingId) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
    
        public long PingId = pingId;
    }
}

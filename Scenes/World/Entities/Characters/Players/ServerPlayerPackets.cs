using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ServerPlayer 
{
    [GamePacket]
    public class CS_PlayerWantShootPacket(Vector2 startPosition, float rotation) : BinaryPacket //TODO del after test
    {
        public Vector2 StartPosition = startPosition;
        public float Rotation = rotation;
    }
}

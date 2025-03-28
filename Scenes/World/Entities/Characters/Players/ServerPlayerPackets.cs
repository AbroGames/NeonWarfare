using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ServerPlayer 
{
    [GamePacket]
    public class CS_UseSkillPacket(long skillId, Vector2 playerPosition, float playerRotation, Vector2 cursorGlobalPosition) : BinaryPacket
    {
        public long SkillId = skillId;
        public Vector2 PlayerPosition = playerPosition;
        public float PlayerRotation = playerRotation;
        public Vector2 CursorGlobalPosition = cursorGlobalPosition;
    }
}

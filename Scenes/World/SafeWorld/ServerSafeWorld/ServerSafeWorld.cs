using NeonWarfare.Scenes.Game.ClientGame;

namespace NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;

public partial class ServerSafeWorld : ServerWorld
{
    public override ClientGame.SC_ChangeWorldPacket.ServerWorldType GetServerWorldType()
    {
        return ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe;
    }
}

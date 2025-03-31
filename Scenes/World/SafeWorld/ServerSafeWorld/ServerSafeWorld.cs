using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scripts.Content;

namespace NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;

public partial class ServerSafeWorld : ServerWorld
{
    public override WorldInfoStorage.WorldType GetServerWorldType()
    {
        return WorldInfoStorage.WorldType.Safe;
    }
}

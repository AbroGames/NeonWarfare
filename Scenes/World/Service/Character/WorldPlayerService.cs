namespace NeonWarfare.Scenes.World.Service.Character;

public partial class WorldPlayerService : WorldCharacterService
{
    
    public void SpawnPlayer(int peerId)
    {
        Tree.MapSurface.AddPlayerCharacter(peerId);
    }
}
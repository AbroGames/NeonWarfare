namespace NeonWarfare.Scenes.World.Service.Characters;

public partial class WorldPlayerService : WorldCharacterService
{
    
    public void SpawnPlayer(int peerId)
    {
        AddPlayerCharacter(peerId);
    }
}
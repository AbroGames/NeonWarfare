using Godot;
using KludgeBox;
using KludgeBox.Networking;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemy : ServerCharacter
{
    [Export] [NotNull] public RayCast2D RayCast { get; private set; }
    
    public override void _Ready()
    {
        Position = Vec(400, 400); //TODO временно для теста, потом удалить, и поменять на нормальные координаты и угол
        Position += Vec(Rand.Range(-300, 300), Rand.Range(-300, 300)); 
        Rotation = Mathf.DegToRad(Rand.Range(0, 360));
        
        //У всех игроков спауним нового врага
        Network.SendToAll(new ClientWorld.SC_EnemySpawnPacket(Nid, Position.X, Position.Y, Rotation));
    }
}

using Godot;
using NeonWarfare;
using NeonWarfare.Scenes.World.Entities.Characters.Players;

namespace NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;

public partial class ServerPlayerProfile
{
    public long PeerId { get; }
    public long UserId { get; set; }
    public bool IsAdmin { get; set; }
    
    public string Name { get; set; }
    public Color Color { get; set; }
    
    //Указаны дефолтные значения, которые имеет игрок при начале игры
    public double MaxHp { get; set; } = 100;
    public double RegenHpSpeed { get; set; } = 1; // hp/sec
    public double MovementSpeed { get; set; } = 250; // in pixels/sec
    public double RotationSpeed { get; set; } = 300; // in degree/sec

    public ServerPlayerProfile(long peerId)
    {
        PeerId = peerId;
    }
}

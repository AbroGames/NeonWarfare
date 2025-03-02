using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;

namespace NeonWarfare.Scenes.World.Entities.Characters;

public partial class ClientCharacter : CharacterBody2D
{
    [Export] [NotNull] public CollisionShape2D CollisionShape { get; private set; }
    [Export] [NotNull] public Sprite2D Sprite { get; private set; }
    
    public long Nid => this.GetChild<NetworkEntityComponent>().Nid;
	
    public double MaxHp { get; set; } = 10000;
    public double Hp { get; set; } = 10000;
    public double RegenHpSpeed { get; set; } = 100; // hp/sec
    public double MovementSpeed { get; set; } = 250; // in pixels/sec
    public double RotationSpeed { get; set; } = 300; // in degree/sec
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public void OnSpawnPacket(Vector2 position, float rotation, Color color)
    {
        Position = position;
        Rotation = rotation;
        Modulate = color;
    }
}

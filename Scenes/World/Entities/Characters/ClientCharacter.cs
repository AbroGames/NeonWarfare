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
    
    public Color Color { get; protected set; } //Цвет, который будет использоваться для снарядов и т.п.
    public double MaxHp { get; set; }
    public double Hp { get; set; }
    public double RegenHpSpeed { get; set; }
    public double MovementSpeed { get; set; }
    public double RotationSpeed { get; set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public virtual void InitOnSpawnPacket(Vector2 position, float rotation, Color color)
    {
        Position = position;
        Rotation = rotation;

        if (color != new Color(0, 0, 0, 0))
        {
            Sprite.Modulate = color;
        }
    }
}

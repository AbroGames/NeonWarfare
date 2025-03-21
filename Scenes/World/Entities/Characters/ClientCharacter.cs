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
	
    public double MaxHp { get; set; } = 100; //TODO Дублируется с ServerCharacter. А фактически дефолтные значения нигде не используются. Мб удалить и там и там?
    public double Hp { get; set; } = 100;
    public double RegenHpSpeed { get; set; } = 1; // hp/sec
    public double MovementSpeed { get; set; } = 250; // in pixels/sec
    public double RotationSpeed { get; set; } = 300; // in degree/sec
    
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

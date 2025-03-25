using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;

namespace NeonWarfare.Scenes.World.Entities.Characters;

public partial class ServerCharacter : CharacterBody2D
{
    [Export] [NotNull] public CollisionShape2D CollisionShape { get; private set; }
    [Export] [NotNull] public Area2D HitBox { get; private set; }

    public long Nid => this.GetChild<NetworkEntityComponent>().Nid;
	
    public double MaxHp { get; set; }
    public double Hp { get; set; }
    public double RegenHpSpeed { get; set; }
    public double MovementSpeed { get; set; }
    public double RotationSpeed { get; set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}

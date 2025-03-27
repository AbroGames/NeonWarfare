using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Actions;

public partial class ServerShotAction : Node2D
{
    
    [Export] [NotNull] public Area2D HitBox { get; private set; }

    public double Damage { get; private set; }
    public float Speed { get; private set; } //pixels/sec
    public float Range { get; private set; } //pixels
    public ServerCharacter Author { get; private set; } 
    public long AuthorPeerId { get; private set; } //PeerId выстрелившего, -1 для вражеского бота, PeerId владельца для союзного бота
    
    private ManualCooldown _destroyCooldown;

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);

        float ttl = Range / Speed;
        _destroyCooldown = new ManualCooldown(ttl, false, true, QueueFree);

        HitBox.AreaEntered += OnHit;
    }

    public void Init(Vector2 startPosition, float rotation)
    {
        Position = startPosition;
        Rotation = rotation;
    }

    public void InitStats(double damage, float speed, float range, ServerCharacter author, long authorPeerId)
    {
        Damage = damage;
        Speed = speed;
        Range = range;
        Author = author;
        AuthorPeerId = authorPeerId;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        _destroyCooldown.Update(delta);
        
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }

    private void OnHit(Area2D area)
    {
        if (area.GetParent() is ServerCharacter character)
        {
            character.OnHit(Damage, Author, AuthorPeerId);
            if (Author != character)
            {
                QueueFree();
            }
        }
        
        //TODO Уничтожать снаряд при столкновении со стеной
    }
}
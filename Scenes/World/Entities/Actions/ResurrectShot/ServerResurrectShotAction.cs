using Godot;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Actions.ResurrectShot;

public partial class ServerResurrectShotAction : Node2D
{
    
    [Export] [NotNull] public Area2D HitBox { get; private set; }

    public double Heal { get; private set; }
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

    public void InitStats(double heal, float speed, float range, ServerCharacter author, long authorPeerId)
    {
        Heal = heal;
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
            if (Author != character)
            {
                character.OnResurrect(Heal, Author, AuthorPeerId);
                QueueFree();
            }
        }

        if (area.GetParent() is StaticBody2D)
        {
            QueueFree();
        }
    }
}
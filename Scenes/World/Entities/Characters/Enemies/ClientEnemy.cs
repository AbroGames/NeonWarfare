using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;
using NeonWarfare.Scripts.Utils.Components;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ClientEnemy : ClientCharacter
{
    public EnemyInfoStorage.EnemyInfo EnemyTemplate { get; private set; } 
    public void InitComponents()
    {
        AddChild(new NetworkInertiaComponent());
        AddChild(new ClientEnemyAudioComponent());
        AddChild(new ClientEnemyDeathComponent());
    }
    
    public void InitStats(EnemyInfoStorage.EnemyInfo enemyInfo)
    {
        EnemyTemplate = enemyInfo;
        
        Color = enemyInfo.Color;
        MaxHp = enemyInfo.MaxHp;
        Hp = MaxHp;
        RegenHpSpeed = enemyInfo.RegenHpSpeed;
        MovementSpeed = enemyInfo.MovementSpeed;
        RotationSpeed = enemyInfo.RotationSpeed;
    }

    protected override void ProcessDamage(SC_DamageCharacterPacket damageCharacterPacket)
    {
        var sfx = EnemyTemplate.AudioProfile?.HitSfx?.Invoke();
        if (sfx is not null)
            Audio2D.PlaySoundAt(sfx, Position, ClientRoot.Instance.Game.World);
    }

    public Color GetColor() => Sprite.Modulate;
    
    public virtual float GetScaleFactor() => (Sprite.Scale.X + Sprite.Scale.Y) / 2;
}

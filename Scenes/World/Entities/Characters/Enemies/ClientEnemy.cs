using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Utils.Components;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ClientEnemy : ClientCharacter
{
    public EnemyInfoStorage.EnemyInfo EnemyTemplate { get; private set; } 
    public void InitComponents()
    {
        AddChild(new NetworkInertiaComponent());
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
}

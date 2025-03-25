using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content;

public static class EnemyInfoStorage
{
    
    public record EnemyInfo(
        Func<PackedScene> ClientScene, 
        Func<PackedScene> ServerScene,
        double MaxHp,
        double RegenHpSpeed,
        double MovementSpeed,
        double RotationSpeed
        );
    
    public enum EnemyType
    {
        Zerg, Shooter, Turtle
    }
    
    private static readonly IReadOnlyDictionary<EnemyType, EnemyInfo> EnemyInfoMap = new Dictionary<EnemyType, EnemyInfo>
    {
        { 
            EnemyType.Zerg, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.ZergEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.ZergEnemy,
                MaxHp: 25,
                RegenHpSpeed: 0,
                MovementSpeed: 220,
                RotationSpeed: 300
            )
        },
        { 
            EnemyType.Shooter, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.ShooterEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.ShooterEnemy,
                MaxHp: 40,
                RegenHpSpeed: 0,
                MovementSpeed: 200,
                RotationSpeed: 280
            )
        },
        { 
            EnemyType.Turtle, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.TurtleEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.TurtleEnemy,
                MaxHp: 400,
                RegenHpSpeed: 8,
                MovementSpeed: 140,
                RotationSpeed: 220
            )
        }
    };
    
    public static EnemyInfo GetEnemyInfo(EnemyType enemyType)
    {
        if (!EnemyInfoMap.TryGetValue(enemyType, out var enemyInfo))
        {
            Log.Error($"Not found EnemyInfo for unknown EnemyType. EnemyType = {enemyType}");
        }
        return enemyInfo;
    }

    public static PackedScene GetClientScene(EnemyType enemyType)
    {
        return GetEnemyInfo(enemyType).ClientScene.Invoke();
    }
    
    public static PackedScene GetServerScene(EnemyType enemyType)
    {
        return GetEnemyInfo(enemyType).ServerScene.Invoke();
    }
}
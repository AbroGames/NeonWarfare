using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content;

public static class EnemyInfoStorage
{
    public record EnemyInfo(
        Func<PackedScene> ClientScene, 
        Func<PackedScene> ServerScene,
        Color Color, //Используется для снарядов и т.п.
        List<ServerCharacter.SkillInfo> Skills,
        double MaxHp,
        double RegenHpSpeed,
        double MovementSpeed,
        double RotationSpeed,
        EnemyAudioProfileStorage.ClientEnemyAudioProfile AudioProfile
        );
    
    public enum EnemyType
    {
        Zerg, Shooter, Turtle
    }
    
    private static readonly IReadOnlyDictionary<EnemyType, EnemyInfo> EnemyInfoByType = new Dictionary<EnemyType, EnemyInfo>
    {
        { 
            EnemyType.Zerg, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.ZergEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.ZergEnemy,
                Color: new Color(1f, 0.9f, 0.46f),
                Skills: [new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 0.3, 0.1, 2, 0.08)], //TODO реализовать через скилл массового урона по области вокруг себя
                MaxHp: 25,
                RegenHpSpeed: 0,
                MovementSpeed: 220,
                RotationSpeed: 300,
                AudioProfile: EnemyAudioProfileStorage.GetEnemyAudioProfile(EnemyAudioProfileStorage.ProfileType.Zergling)
            )
        },
        { 
            EnemyType.Shooter, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.ShooterEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.ShooterEnemy,
                Color: new Color(1f, 0.38f, 0),
                Skills: [new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 1, 1, 1, 1)],
                MaxHp: 40,
                RegenHpSpeed: 0,
                MovementSpeed: 200,
                RotationSpeed: 280,
                AudioProfile: EnemyAudioProfileStorage.GetEnemyAudioProfile(EnemyAudioProfileStorage.ProfileType.Hydralisk)
            )
        },
        { 
            EnemyType.Turtle, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.TurtleEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.TurtleEnemy,
                Color: new Color(1f, 0, 0),
                Skills: [
                    new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 1, 1, 1, 1.5),
                    new ServerCharacter.SkillInfo(ShotgunSkill.SkillTypeConst, 0.2, 1, 1, 1),
                    new ServerCharacter.SkillInfo(DoubleShotSkill.SkillTypeConst, 5, 1, 1, 1),
                ],
                MaxHp: 800,
                RegenHpSpeed: 20,
                MovementSpeed: 140,
                RotationSpeed: 220,
                AudioProfile: EnemyAudioProfileStorage.GetEnemyAudioProfile(EnemyAudioProfileStorage.ProfileType.Ultralisk)
            )
        }
    };
    
    public static EnemyInfo GetEnemyInfo(EnemyType enemyType)
    {
        if (!EnemyInfoByType.TryGetValue(enemyType, out var enemyInfo))
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
using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

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
        ClientEnemyAudioProfile AudioProfile
        );
    
    public record ClientEnemyAudioProfile(
        double VoicePeriod,
        Func<ClientEnemy, bool> CanDoVoice, 
        Func<PlaybackOptions> SpawnVoice = null,
        Func<PlaybackOptions> DeathVoice = null,
        Func<PlaybackOptions> NormalVoice = null,
        Func<PlaybackOptions> DeathSfx = null,
        Func<PlaybackOptions> HitSfx = null
    );
    
    public enum EnemyType
    {
        Zerg, Shooter, Turtle, Boss
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
                MovementSpeed: 260,
                RotationSpeed: 300,
                AudioProfile: new ClientEnemyAudioProfile(
                    VoicePeriod: 1,
                    CanDoVoice: _ => Rand.Chance(0.5),
                    SpawnVoice: null,
                    DeathVoice: () => new PlaybackOptions(Sfx.ZerglingDeath, 0.2f),
                    NormalVoice: () => new PlaybackOptions(Sfx.ZerglingYes, 0.2f),
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionSmall, 0.1f),
                    HitSfx: () => new PlaybackOptions(Sfx.HitFlesh, 0.3f)
                )
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
                AudioProfile: new ClientEnemyAudioProfile(
                    VoicePeriod: 2,
                    CanDoVoice: _ => Rand.Chance(0.5),
                    SpawnVoice: null,
                    DeathVoice: () => new PlaybackOptions(Sfx.HydraliskDeath, 0.3f),
                    NormalVoice: () => new PlaybackOptions(Sfx.HydraliskYes, 0.2f),
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionSmall, 0.2f),
                    HitSfx: () => new PlaybackOptions(Sfx.HitFlesh, 0.3f)
                )
            )
        },
        { 
            EnemyType.Turtle, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.TurtleEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.TurtleEnemy,
                Color: new Color(1f, 0, 0),
                Skills: [
                    new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 0.65, 1, 1, 1.5),
                    new ServerCharacter.SkillInfo(ShotgunSkill.SkillTypeConst, 0.4, 0.8, 1, 1),
                    new ServerCharacter.SkillInfo(DoubleShotSkill.SkillTypeConst, 5, 1, 1, 1),
                ],
                MaxHp: 800,
                RegenHpSpeed: 20,
                MovementSpeed: 140,
                RotationSpeed: 220,
                AudioProfile: new ClientEnemyAudioProfile(
                    VoicePeriod: 10,
                    CanDoVoice: _ => false,
                    SpawnVoice: () => new PlaybackOptions(
                        Path:Sfx.UltraliskWhat,
                        Volume: 1f,
                        MaxDistance: 10000f, // мы хотим слышать спавн <s>ультралиска</s> черепахи с очень большого расстояния
                        Attenuation: 1f
                    ),
                    DeathVoice: () => new PlaybackOptions(Sfx.UltraliskWhat, 1f),
                    NormalVoice: () => null,
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionMedium, 1f),
                    HitSfx: () => new PlaybackOptions(Sfx.Hit, 0.3f)
                )
            )
        },
        { 
            EnemyType.Boss, 
            new EnemyInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.BossEnemy, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.BossEnemy,
                Color: new Color(0.5f, 0, 1f),
                Skills: [
                    new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 5, 12, 0.8, 5),
                    new ServerCharacter.SkillInfo(ShotgunSkill.SkillTypeConst, 0.25, 0.4, 1.5, 1.5),
                    new ServerCharacter.SkillInfo(DoubleShotSkill.SkillTypeConst, 0.1, 0.25, 1.5, 1),
                ],
                MaxHp: 4000,
                RegenHpSpeed: 40,
                MovementSpeed: 200,
                RotationSpeed: 400,
                AudioProfile: new ClientEnemyAudioProfile(
                    VoicePeriod: 10,
                    CanDoVoice: _ => false,
                    SpawnVoice: () => new PlaybackOptions(
                        Path:Sfx.UltraliskWhat,
                        Volume: 1.3f,
                        MaxDistance: 10000f, // мы хотим слышать спавн <s>ультралиска</s> черепахи с очень большого расстояния
                        Attenuation: 1.3f
                    ),
                    DeathVoice: () => new PlaybackOptions(Sfx.UltraliskWhat, 1.3f),
                    NormalVoice: () => null,
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionMedium, 1.3f),
                    HitSfx: () => new PlaybackOptions(Sfx.Hit, 0.4f)
                )
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
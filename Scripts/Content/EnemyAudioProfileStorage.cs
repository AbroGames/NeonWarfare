#region

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

#endregion

namespace NeonWarfare.Scripts.Content;

public static class EnemyAudioProfileStorage
{
    public record PlaybackOptions(string Path, 
        float Volume,
        float MaxDistance = 3000f,
        float PanningStrength = 3f,
        float Attenuation = 1f
        ); 
    
    public record ClientEnemyAudioProfile(
        double VoicePeriod,
        Func<ClientEnemy, bool> CanDoVoice, 
        Func<PlaybackOptions> SpawnVoice = null,
        Func<PlaybackOptions> DeathVoice = null,
        Func<PlaybackOptions> NormalVoice = null,
        Func<PlaybackOptions> DeathSfx = null
        );
    
    public enum ProfileType
    {
        Zergling,
        Hydralisk,
        Ultralisk
    }

    private static readonly IReadOnlyDictionary<ProfileType, ClientEnemyAudioProfile> EnemyAudioProfileByType =
        new Dictionary<ProfileType, ClientEnemyAudioProfile>
        {
            {
                ProfileType.Zergling,
                new ClientEnemyAudioProfile(
                    VoicePeriod: 1,
                    CanDoVoice: _ => Rand.Chance(0.5),
                    SpawnVoice: null,
                    DeathVoice: () => new PlaybackOptions(Sfx.ZerglingDeath, 0.2f),
                    NormalVoice: () => new PlaybackOptions(Sfx.ZerglingYes, 0.2f),
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionSmall, 0.1f)
                )
            },
    
            {
                ProfileType.Hydralisk,
                new ClientEnemyAudioProfile(
                    VoicePeriod: 2,
                    CanDoVoice: _ => Rand.Chance(0.5),
                    SpawnVoice: null,
                    DeathVoice: () => new PlaybackOptions(Sfx.HydraliskDeath, 1f),
                    NormalVoice: () => new PlaybackOptions(Sfx.HydraliskYes, 0.2f),
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionSmall, 0.2f)
                )
            },
    
            {
                ProfileType.Ultralisk,
                new ClientEnemyAudioProfile(
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
                    DeathSfx: () => new PlaybackOptions(Sfx.ZergExplosionMedium, 1f)
                )
            }
        };
    
    public static ClientEnemyAudioProfile GetEnemyAudioProfile(ProfileType enemyType)
    {
        if (!EnemyAudioProfileByType.TryGetValue(enemyType, out var enemyInfo))
        {
            Log.Error($"Unable to find audio profile for unknown ProfileType. ProfileType = {enemyType}");
        }
        return enemyInfo;
    }
}
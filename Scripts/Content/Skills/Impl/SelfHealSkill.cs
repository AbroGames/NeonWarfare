using System.Text.Json;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Actions;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;
using NeonWarfare.Scripts.KludgeBox.Networking;
using ClientHealShotAction = NeonWarfare.Scenes.World.Entities.Actions.HealShot.ClientHealShotAction;
using ServerHealShotAction = NeonWarfare.Scenes.World.Entities.Actions.HealShot.ServerHealShotAction;

namespace NeonWarfare.Scripts.Content.Skills.Impl;

public class SelfHealSkill : Skill
{
    public SelfHealSkill() : base(SkillTypeConst)
    {
        AudioProfile = new SkillAudioProfile(
            BeginSound: () => new PlaybackOptions(
                Path: Sfx.HealLaunch, 
                Volume: 1f
            )
        );
    }

    public const string SkillTypeConst = "SelfHeal";
    
    private const double Heal = 50;
    
    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        useInfo.Author.OnHeal(Heal*useInfo.DamageFactor, useInfo.Author, useInfo.AuthorPeerId, SkillType);
    }

    public override void OnClientUse(ClientSkillUseInfo useInfo)
    {
        PlaybackOptions playback = AudioProfile?.BeginSound?.Invoke();
        if (playback is not null)
        {
            Audio2D.PlaySoundAt(playback, useInfo.CharacterPosition, useInfo.World);
        }
    }

    public override bool CheckEnemyCanUse(ServerEnemy enemy, double rangeFactor)
    {
        return false;
    }
}
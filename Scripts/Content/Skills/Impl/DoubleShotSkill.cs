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
using ClientShotAction = NeonWarfare.Scenes.World.Entities.Actions.Shot.ClientShotAction;
using ServerShotAction = NeonWarfare.Scenes.World.Entities.Actions.Shot.ServerShotAction;

namespace NeonWarfare.Scripts.Content.Skills.Impl;

public class DoubleShotSkill : Skill
{
    public DoubleShotSkill() : base(SkillTypeConst)
    {
        AudioProfile = new SkillAudioProfile(
            BeginSound: () => new PlaybackOptions(
                Path: Sfx.LaserShot, 
                Volume: 1f,
                PitchScale: 1f,
                MaxDistance: 5000,
                Attenuation: 0.9f
            )
        );
    }

    public const string SkillTypeConst = "DoubleShot";
    
    private const ActionInfoStorage.ActionType ActionType = ActionInfoStorage.ActionType.Shot;
    private const double Speed = 1900;
    private const double Range = 3500;
    private const double Damage = 90;
    
    private const double EnemyCheckRange = 2000;
    
    private const float DeltaPos = 25;
    private const float Scale = 1.5f;

    private record PacketCustomParams(float Speed, long Nid1, long Nid2);
    
    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        long nid1 = CreateServerShotAction(useInfo, DeltaPos);
        long nid2 = CreateServerShotAction(useInfo, -DeltaPos);

        string customParams = JsonSerializer.Serialize(new PacketCustomParams(
            Speed: (float) (Speed*useInfo.SpeedFactor),
            Nid1: nid1,
            Nid2: nid2
            ));
        Network.SendToAll(new ClientWorld.SC_UseSkillPacket(
            skillType: SkillType, 
            nid: -1, // Не используется, nid всех снарядов передаются в customParams
            characterPosition: useInfo.CharacterPosition, 
            characterRotation: useInfo.CharacterRotation, 
            cursorGlobalPosition: useInfo.CursorGlobalPosition, 
            color: useInfo.Author.Color, 
            customParams: customParams
            ));
    }

    private long CreateServerShotAction(ServerSkillUseInfo useInfo, float deltaPosX)
    {
        ServerShotAction shotAction = useInfo.World.CreateNetworkEntity<ServerShotAction>(ActionInfoStorage.GetServerScene(ActionType));
        long nid = shotAction.GetChild<NetworkEntityComponent>().Nid;
        Vector2 deltaVector = new Vector2(deltaPosX, 0).Rotated(useInfo.CharacterRotation);
        shotAction.Init(useInfo.CharacterPosition + deltaVector, useInfo.CharacterRotation);
        shotAction.Scale *= new Vector2(Scale, Scale);
        shotAction.InitStats(
            damage: Damage*useInfo.DamageFactor, 
            speed: (float) (Speed*useInfo.SpeedFactor), 
            range: (float) (Range*useInfo.RangeFactor), 
            author: useInfo.Author, 
            authorPeerId: useInfo.AuthorPeerId,
            skillType: SkillTypeConst);
        useInfo.World.AddChild(shotAction);
        
        return nid;
    }

    public override void OnClientUse(ClientSkillUseInfo useInfo)
    {
        PacketCustomParams customParams = JsonSerializer.Deserialize<PacketCustomParams>(useInfo.CustomParams);
        CreateClientShotAction(useInfo, customParams.Speed, customParams.Nid1, DeltaPos);
        CreateClientShotAction(useInfo, customParams.Speed, customParams.Nid2, -DeltaPos);
        
        PlaybackOptions playback = AudioProfile?.BeginSound?.Invoke();
        if (playback is not null)
        {
            Audio2D.PlaySoundAt(playback, useInfo.CharacterPosition, useInfo.World);
        }
    }

    private void CreateClientShotAction(ClientSkillUseInfo useInfo, float speed, long nid, float deltaPosX)
    {
        ClientShotAction shotAction = useInfo.World.CreateNetworkEntity<ClientShotAction>(ActionInfoStorage.GetClientScene(ActionType), nid);
        Vector2 deltaVector = new Vector2(deltaPosX, 0).Rotated(useInfo.CharacterRotation);
        shotAction.Init(useInfo.CharacterPosition + deltaVector, useInfo.CharacterRotation);
        shotAction.Scale *= new Vector2(Scale, Scale);
        shotAction.InitStats(speed, useInfo.Color);
        useInfo.World.AddChild(shotAction);
    }

    public override bool CheckEnemyCanUse(ServerEnemy enemy, double rangeFactor)
    {
        return CheckEnemyRayCastAndDistToTarget(enemy, EnemyCheckRange * rangeFactor);
    }
}
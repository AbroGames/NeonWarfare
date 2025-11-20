using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Godot.Collections;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Actions;
using NeonWarfare.Scenes.World.Entities.Characters;
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

public class QuickShotSkill : Skill
{
    public QuickShotSkill() : base(SkillTypeConst)
    {
        AudioProfile = new SkillAudioProfile(
            BeginSound: () => new PlaybackOptions(
                Path: Sfx.HandgunShot, 
                Volume: 0.5f,
                PitchScale: 1f
                )
            );
    }

    public const string SkillTypeConst = "QuickShot";
    
    private const ActionInfoStorage.ActionType ActionType = ActionInfoStorage.ActionType.Shot;
    private const double Speed = 5000;
    private const double Range = 2000;
    private const double Damage = 50;
    private const float Spread = Mathf.Pi / 25;
    
    private const double EnemyCheckRange = 800;
    private RandomNumberGenerator _random = new();

    private record PacketCustomParams(Vector2 HitPos);

    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        ServerRoot.Instance.Game.World.WaitForPhysics(() => Deferrable_OnServerUse(useInfo));
    }
    
    private void Deferrable_OnServerUse(ServerSkillUseInfo useInfo)
    {
        var worldSpace = ServerRoot.Instance.Game.World.GetWorld2D().DirectSpaceState;
        var direction = useInfo.Author.Up().Rotated(_random.RandfRange(-Spread/2, Spread/2));//Vector2.Up.Rotated(useInfo.CharacterRotation);
        var raycastQuery = PhysicsRayQueryParameters2D.Create(
            useInfo.Author.Position, 
            useInfo.Author.Position + direction * ((float)Range * (float)useInfo.RangeFactor) * _random.RandfRange(0.95f, 1.05f),
            exclude: [useInfo.Author.GetRid()]);
        
        var raycastResult = worldSpace.IntersectRay(raycastQuery);
        Vector2 hitPosition;
        if (raycastResult.Any())
        {
            hitPosition = raycastResult["position"].AsVector2();
            var target = raycastResult["collider"].AsGodotObject();
            Log.Info($"(srv) Hit {target}");
            if (target is ServerCharacter targetCharacter)
            {
                targetCharacter.OnHit(Damage*useInfo.DamageFactor, useInfo.Author, useInfo.AuthorPeerId, SkillTypeConst);
            }
        }
        else
        {
            hitPosition = useInfo.Author.Position + direction * (float)Range * (float)useInfo.RangeFactor;
        }

        var customParams = GD.VarToStr(hitPosition);
        
        Network.SendToAll(new ClientWorld.SC_UseSkillPacket(
            skillType: SkillType, 
            nid: -1, 
            characterPosition: useInfo.CharacterPosition, 
            characterRotation: useInfo.CharacterRotation, 
            cursorGlobalPosition: useInfo.CursorGlobalPosition, 
            color: useInfo.Author.Color, 
            customParams: customParams
        ));
    }

    public override void OnClientUse(ClientSkillUseInfo useInfo)
    {
        var customParams = new PacketCustomParams(
            HitPos: GD.StrToVar(useInfo.CustomParams).AsVector2()
        );
        //PacketCustomParams customParams = JsonSerializer.Deserialize<PacketCustomParams>(useInfo.CustomParams);
        var beamVector = customParams.HitPos - useInfo.CharacterPosition;
        var (beam, tween) = Fx.CreateBulletBeamFx(beamVector, 5f, useInfo.Color, 0.3);
        beam.Modulate = Col(1) with { A = 0.5f };
        beam.Position = useInfo.CharacterPosition;
        tween.Finished += () => beam.QueueFree();
        useInfo.World.AddChild(beam);
        

        var hit = Fx.CreateSpriteBulletHitFx();
        hit.Modulate = useInfo.Color;
        hit.Rotation = beamVector.Angle();
        hit.Position = customParams.HitPos;
        hit.AnimationFinished += () => hit.QueueFree();
        useInfo.World.AddChild(hit);
        
        PlaybackOptions playback = AudioProfile?.BeginSound?.Invoke();
        if (playback is not null)
        {
            Audio2D.PlaySoundAt(playback, useInfo.CharacterPosition, useInfo.World);
        }
    }


    public override bool CheckEnemyCanUse(ServerEnemy enemy, double rangeFactor)
    {
        return CheckEnemyRayCastAndDistToTarget(enemy, EnemyCheckRange * rangeFactor);
    }
}
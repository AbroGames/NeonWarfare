
using System;
using System.Text.Json;
using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Actions;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;
using ClientShotAction = NeonWarfare.Scenes.World.Entities.Actions.Shot.ClientShotAction;
using ServerShotAction = NeonWarfare.Scenes.World.Entities.Actions.Shot.ServerShotAction;

namespace NeonWarfare.Scripts.Content.Skills.Impl;

public class ShotgunSkill() : Skill(SkillTypeConst)
{
    public const string SkillTypeConst = "Shotgun";
    
    private const ActionInfoStorage.ActionType ActionType = ActionInfoStorage.ActionType.Shot;
    private const double Speed = 1300;
    private const double Range = 700;
    private const double Damage = 15;
    private const int Count = 5;
    private const int Spread = 20; //degrees in one direction
    
    private const double EnemyCheckRange = 600;

    private record ShotInfo(long Nid, float Rotation);
    private record PacketCustomParams(float Speed, ShotInfo[] ShotInfos);
    
    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        ShotInfo[] shotInfos = new ShotInfo[Count];
        for (int i = 0; i < Count; ++i)
        {
            ServerShotAction shotAction = useInfo.World.CreateNetworkEntity<ServerShotAction>(ActionInfoStorage.GetServerScene(ActionType));
            long nid = shotAction.GetChild<NetworkEntityComponent>().Nid;
            float rotation = useInfo.CharacterRotation + Mathf.DegToRad(Rand.Range(-Spread, Spread));
            shotAction.Init(useInfo.CharacterPosition, rotation);         
            shotAction.InitStats(
                damage: Damage*useInfo.DamageFactor, 
                speed: (float) (Speed*useInfo.SpeedFactor), 
                range: (float) (Range*useInfo.RangeFactor), 
                author: useInfo.Author, 
                authorPeerId: useInfo.AuthorPeerId);
            useInfo.World.AddChild(shotAction);
            
            shotInfos[i] = new ShotInfo(nid, rotation);
        }

        string customParams = JsonSerializer.Serialize(new PacketCustomParams(
            Speed: (float) Speed,
            ShotInfos: shotInfos
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

    public override void OnClientUse(ClientSkillUseInfo useInfo)
    {
        PacketCustomParams customParams = JsonSerializer.Deserialize<PacketCustomParams>(useInfo.CustomParams);

        for (int i = 0; i < customParams.ShotInfos.Length; i++)
        {
            ClientShotAction shotAction = useInfo.World.CreateNetworkEntity<ClientShotAction>(ActionInfoStorage.GetClientScene(ActionType), customParams.ShotInfos[i].Nid);
            shotAction.Init(useInfo.CharacterPosition, customParams.ShotInfos[i].Rotation);
            shotAction.InitStats(customParams.Speed, useInfo.Color);
            useInfo.World.AddChild(shotAction);
        }
    }

    public override bool CheckEnemyCanUse(ServerEnemy enemy, double rangeFactor)
    {
        return CheckEnemyRayCastAndDistToTarget(enemy, EnemyCheckRange * rangeFactor);
    }
}
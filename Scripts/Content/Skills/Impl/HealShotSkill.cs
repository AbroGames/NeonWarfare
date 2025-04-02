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
using NeonWarfare.Scripts.KludgeBox.Networking;
using ClientHealShotAction = NeonWarfare.Scenes.World.Entities.Actions.HealShot.ClientHealShotAction;
using ServerHealShotAction = NeonWarfare.Scenes.World.Entities.Actions.HealShot.ServerHealShotAction;

namespace NeonWarfare.Scripts.Content.Skills.Impl;

public class HealShotSkill() : Skill(SkillTypeConst)
{

    public const string SkillTypeConst = "HealShot";
    
    private const ActionInfoStorage.ActionType ActionType = ActionInfoStorage.ActionType.HealShot;
    private const double Speed = 1800;
    private const double Range = 1500;
    private const double Heal = 50;
    
    private const double EnemyCheckRange = 1200;

    private record PacketCustomParams(float Speed);
    
    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        ServerHealShotAction shotAction = useInfo.World.CreateNetworkEntity<ServerHealShotAction>(ActionInfoStorage.GetServerScene(ActionType));
        long nid = shotAction.GetChild<NetworkEntityComponent>().Nid;
        shotAction.Init(useInfo.CharacterPosition, useInfo.CharacterRotation);
        shotAction.InitStats(
            heal: Heal*useInfo.DamageFactor, 
            speed: (float) (Speed*useInfo.SpeedFactor), 
            range: (float) (Range*useInfo.RangeFactor), 
            author: useInfo.Author, 
            authorPeerId: useInfo.AuthorPeerId,
            skillType: SkillTypeConst);
        useInfo.World.AddChild(shotAction);

        string customParams = JsonSerializer.Serialize(new PacketCustomParams(
            Speed: (float) Speed
            ));
        Network.SendToAll(new ClientWorld.SC_UseSkillPacket(
            skillType: SkillType, 
            nid: nid, 
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
        
        ClientHealShotAction shotAction = useInfo.World.CreateNetworkEntity<ClientHealShotAction>(ActionInfoStorage.GetClientScene(ActionType), useInfo.Nid);
        shotAction.Init(useInfo.CharacterPosition, useInfo.CharacterRotation);
        shotAction.InitStats(customParams.Speed);
        useInfo.World.AddChild(shotAction);
    }

    public override bool CheckEnemyCanUse(ServerEnemy enemy, double rangeFactor)
    {
        GodotObject collider = enemy.RayCast.GetCollider();
        return collider is ServerEnemy serverEnemy &&
               enemy.DistanceTo(serverEnemy) < EnemyCheckRange * rangeFactor;
    }
}
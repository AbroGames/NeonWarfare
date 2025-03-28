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

namespace NeonWarfare.Scripts.Content.Skills.Impl;

public class DefaultShotSkill() : Skill(SkillTypeConst)
{

    public const string SkillTypeConst = "DefaultShot";
    
    private const ActionInfoStorage.ActionType ActionType = ActionInfoStorage.ActionType.Shot;
    private const double Speed = 2000;
    private const double Range = 4000;
    private const double Damage = 50;
    
    private const double EnemyCheckRange = 1500;

    private record PacketCustomParams(float Speed);
    
    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        ServerShotAction shotAction = useInfo.World.CreateNetworkEntity<ServerShotAction>(ActionInfoStorage.GetServerScene(ActionType));
        long nid = shotAction.GetChild<NetworkEntityComponent>().Nid;
        shotAction.Init(useInfo.CharacterPosition, useInfo.CharacterRotation); //TODO Сделать небольшое смещение на половину длины снаряда, чтобы он не спавнился в центре персонажа (или сделать смещенеи на половину character.sprite.size*character.sprite.scale). И у shotgun аналогично
        shotAction.InitStats(
            damage: Damage*useInfo.DamageFactor, 
            speed: (float) (Speed*useInfo.SpeedFactor), 
            range: (float) (Range*useInfo.RangeFactor), 
            author: useInfo.Author, 
            authorPeerId: useInfo.AuthorPeerId);
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
        
        ClientShotAction shotAction = useInfo.World.CreateNetworkEntity<ClientShotAction>(ActionInfoStorage.GetClientScene(ActionType), useInfo.Nid);
        shotAction.Init(useInfo.CharacterPosition, useInfo.CharacterRotation);
        shotAction.InitStats(customParams.Speed, useInfo.Color);
        useInfo.World.AddChild(shotAction);
    }

    public override bool CheckEnemyCanUse(ServerEnemy enemy)
    {
        return CheckEnemyRayCastAndDistToTarget(enemy, EnemyCheckRange);
    }
}
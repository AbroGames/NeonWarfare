using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame
{

    [GamePacket]
    public class SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType loadingScreenType) : BinaryPacket
    {
        public LoadingScreenBuilder.LoadingScreenType LoadingScreenType = loadingScreenType;
    }

    [GamePacket]
    public class SC_ClearLoadingScreenPacket : BinaryPacket;
    
    [GamePacket]
    public class SC_ChangeWorldPacket(SC_ChangeWorldPacket.ServerWorldType worldType) : BinaryPacket
    {
        public ServerWorldType WorldType = worldType;
        public PackedScene WorldMainScene => WorldScenesMap[WorldType].Invoke();
        
        public enum ServerWorldType
        {
            Safe, Battle
        }
        
        private static readonly Dictionary<ServerWorldType, Func<PackedScene>> WorldScenesMap = new() 
        {
            { ServerWorldType.Safe, () => ClientRoot.Instance.PackedScenes.SafeWorldMainScene },
            { ServerWorldType.Battle, () => ClientRoot.Instance.PackedScenes.BattleWorldMainScene }
        };
    }
    
    [GamePacket]
    public class SC_AddAllyProfilePacket(long peerId, string name, Color color) : BinaryPacket
    {
        public long PeerId = peerId;
        public string Name = name;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_RemoveAllyProfilePacket(long peerId) : BinaryPacket
    {
        public long PeerId = peerId;
    }
    
    [GamePacket]
    public class SC_AddPlayerProfilePacket(long peerId, string name, Color color) : BinaryPacket
    {
        public long PeerId = peerId;
        public string Name = name;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_ChangeSettingsPacket(bool friendlyFire, bool enemyFriendlyFire, bool healEnemyByPlayer, bool healPlayerByEnemy, 
        bool resurrectEnemyByPlayer, bool resurrectPlayerByEnemy, double skillCooldownFactorWhileDead)
        : BinaryPacket
    {
        public bool FriendlyFire = friendlyFire;
        public bool EnemyFriendlyFire = enemyFriendlyFire;
        public bool HealEnemyByPlayer = healEnemyByPlayer;
        public bool HealPlayerByEnemy = healPlayerByEnemy;
        public bool ResurrectEnemyByPlayer = resurrectEnemyByPlayer;
        public bool ResurrectPlayerByEnemy = resurrectPlayerByEnemy;
        public double SkillCooldownFactorWhileDead = skillCooldownFactorWhileDead;
    }
}

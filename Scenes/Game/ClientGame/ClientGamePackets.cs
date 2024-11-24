using System.Collections.Generic;
using Godot;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ClientGame
{
    
    [GamePacket]
    public class SC_WaitBattleEndPacket : BinaryPacket;
    
    [GamePacket]
    public class SC_ChangeWorldPacket(SC_ChangeWorldPacket.ServerWorldType worldType) : BinaryPacket
    {
        
        public enum ServerWorldType
        {
            Safe, Battle
        }
        
        public static readonly Dictionary<ServerWorldType, PackedScene> WorldScenesMap = new() 
        {
            { ServerWorldType.Safe, ClientRoot.Instance.PackedScenes.SafeWorldMainScene },
            { ServerWorldType.Battle, ClientRoot.Instance.PackedScenes.BattleWorldMainScene }
        };
        
        
        public ServerWorldType WorldType = worldType;
    }
}
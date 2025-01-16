using System.Collections.Generic;
using Godot;
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
        
        public enum ServerWorldType
        {
            Safe, Battle
        }
        
        public ServerWorldType WorldType = worldType;
    }
    
    [GamePacket]
    public class SC_AddPlayerProfilePacket(long peerId) : BinaryPacket
    {
        public long PeerId = peerId;
    }
    
    [GamePacket]
    public class SC_AddAllyProfilePacket(long peerId) : BinaryPacket
    {
        public long PeerId = peerId;
    }
    
    [GamePacket]
    public class SC_RemoveAllyProfilePacket(long peerId) : BinaryPacket
    {
        public long PeerId = peerId;
    }
}

using System.Collections.Generic;
using Godot;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;

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
}
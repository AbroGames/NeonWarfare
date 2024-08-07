using Godot;
using KludgeBox;
using KludgeBox.Events;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare;

public partial class Ally : Character
{

    [EventListener(ListenerSide.Client)]
    public void OnServerPositionEntityPacket(ServerPositionEntityPacket serverPositionEntityPacket)
    {
        Log.Warning("SERVER POSITION: " + serverPositionEntityPacket);
    }
    
    /*[EventListener(ListenerSide.Client)]
    public static void OnStaticServerPositionEntityPacket(ServerPositionEntityPacket serverPositionEntityPacket)
    {
        Log.Warning("STATIC SERVER POSITION: " + serverPositionEntityPacket);
    }*/
}
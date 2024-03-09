using System;
using System.Diagnostics;
using System.Linq;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using Newtonsoft.Json.Serialization;

namespace NeoVector;

[GameService]
public class ServerService
{

    [EventListener]
    public void OnPeerConnectedServerEvent(PeerConnectedServerEvent peerConnectedServerEvent)
    {
        Log.Debug($"PeerConnectedServerEvent: {peerConnectedServerEvent.Id}");
    }
    
    [EventListener]
    public void OnPeerDisconnectedServerEvent(PeerDisconnectedServerEvent peerDisconnectedServerEvent)
    {
        Log.Debug($"PeerDisconnectedServerEvent: {peerDisconnectedServerEvent.Id}");
    }
    
    [EventListener]
    public void OnServerReadyEvent(ServerReadyEvent serverReadyEvent)
    {
        Server server = serverReadyEvent.Server;

        server.CheckParentIsDeadTimer.Ready += () => EventBus.Publish(new ServerCheckParentIsDeadEvent(server));
        Log.Info("Server ready!");
    }
    
    [EventListener]
    public void OnServerProcessEvent(ServerProcessEvent serverProcessEvent)
    {
        Server server = serverProcessEvent.Server;
        double delta = serverProcessEvent.Delta;

        server.CheckParentIsDeadTimer.Update(delta);
    }
    
    [EventListener]
    public void OnServerCheckParentIsDeadEvent(ServerCheckParentIsDeadEvent serverCheckParentIsDeadEvent)
    {
        Server server = serverCheckParentIsDeadEvent.Server;
        int? parentPid = server.ServerParams.ParentPid;
        
        if (parentPid.HasValue && !Process.GetProcesses().Any(x => x.Id == parentPid.Value))
        {
            Log.Error($"Parent process {parentPid.Value} is dead. Shutdown server.");
            EventBus.Publish(new ShutDownEvent());
            server.GetTree().Quit();
        }
    }
    
}
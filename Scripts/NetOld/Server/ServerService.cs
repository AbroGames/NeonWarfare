using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;
using NeonWarfare;

namespace NeoVector;

[GameService]
public class ServerService
{
    
    [EventListener]
    public void OnServerReadyEvent(ServerReadyEvent serverReadyEvent)
    {
        Server server = serverReadyEvent.Server;

        server.IsParentDeadChecker.Ready += () => EventBus.Publish(new ServerCheckParentIsDeadEvent(server));
        
        var safeWorld = NeonWarfare.Root.Instance.PackedScenes.Main.SafeWorld;
        NeonWarfare.Root.Instance.MainSceneContainer.ChangeStoredNode(safeWorld.Instantiate());
        
        Log.Info("Server ready!");
    }
    
    [EventListener]
    public void OnServerProcessEvent(ServerProcessEvent serverProcessEvent)
    {
        Server server = serverProcessEvent.Server;
        double delta = serverProcessEvent.Delta;

        server.IsParentDeadChecker.Update(delta);
    }
    
    [EventListener]
    public void OnServerCheckParentIsDeadEvent(ServerCheckParentIsDeadEvent serverCheckParentIsDeadEvent)
    {
        Server server = serverCheckParentIsDeadEvent.Server;
        int? parentPid = server.ServerParams.ParentPid;
        
        if (parentPid.HasValue && !Process.GetProcesses().Any(x => x.Id == parentPid.Value))
        {
            Log.Error($"Parent process {parentPid.Value} is dead. Shutdown server.");
            MenuButtonsService.ShutDown();
            server.GetTree().Quit();
        }
    }
    
}
using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class RootInitService
{

    [EventListener]
    public void OnRootInitEvent(RootInitEvent rootInitEvent)
    {
        EventBus.Publish(new LogCmdArgsRequest());
        EventBus.Publish(new NetworkInitRequest());
        
        if (OS.GetCmdlineArgs().Contains(InitServerService.ServerFlag))
        {
            EventBus.Publish(new InitServerRequest());
        }
        else
        {
            EventBus.Publish(new InitClientRequest());
        }
    }
    
    [EventListener]
    public void OnLogCmdArgsRequest(LogCmdArgsRequest logCmdArgsRequest)
    {
        if (!OS.GetCmdlineArgs().IsEmpty())
        {
            Log.Info("Cmd args: " + OS.GetCmdlineArgs().Join());
        }
        else
        {
            Log.Info("Not have cmd args");
        }
    }

    [EventListener]
    public void OnNetworkInitRequest(NetworkInitRequest networkInitRequest)
    {
        Network.Init();
    }

    [EventListener]
    public void OnInitClientRequest(InitClientRequest initClientRequest)
    {
        var mainMenu = Root.Instance.PackedScenes.Main.MainMenu;
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(mainMenu.Instantiate());
    }
}
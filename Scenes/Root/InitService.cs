using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class InitService
{

    [EventListener]
    public void OnInitRequest(InitRequest initRequest)
    {
        EventBus.Publish(new LogCmdArgsRequest());

        if (OS.GetCmdlineArgs().Contains("--server"))
        {
            EventBus.Publish(new InitServerRequest());
        }
        else
        {
            EventBus.Publish(new InitClientRequest());
        }
    }
    
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
}
﻿using Godot;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Utils;

namespace NeonWarfare;

public static class RootService
{
    public static void CommonInit(MultiplayerApi multiplayerApi, ListenerSide listenerSide)
    {
        ExceptionHandlerService.AddExceptionHandlerForUnhandledException();
        CmdArgsService.LogCmdArgs();
        EventBus.Init(listenerSide);
        Netplay.Initialize(multiplayerApi as SceneMultiplayer);
    }
}
using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scripts.Content;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld : Node2D
{
    public NavigationService NavigationService { get; protected set; }
    
    public void Init(List<ServerPlayerProfile> playerProfiles)
    {
        NavigationService = new NavigationService();
        AddChild(NavigationService);
        InitMap();
        InitPlayers(playerProfiles);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        CheckAllDeadAndRestart();
        
        while (_physicsActions.TryDequeue(out var action))
        {
            action();
        }
    }

    public virtual WorldInfoStorage.WorldType GetServerWorldType()
    {
        return WorldInfoStorage.WorldType.Unknown;
    }

    private Queue<Action> _physicsActions = new();
    public void WaitForPhysics(Action action)
    {
        _physicsActions.Enqueue(action);
    }
}

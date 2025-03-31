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
    }

    public virtual WorldInfoStorage.WorldType GetServerWorldType()
    {
        return WorldInfoStorage.WorldType.Unknown;
    }
}

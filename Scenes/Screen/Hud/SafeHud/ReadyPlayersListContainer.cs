using Godot;
using System;
using System.Linq;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class ReadyPlayersListContainer : MarginContainer
{
    [Export] [NotNull] private PackedScene _listItemScene { get; set; }
    [Export] [NotNull] private Control _playersContainer { get; set; }
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void RebuildReadyPlayersList(long[] readyClients)
    {
        foreach (Node child in _playersContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        var allPlayers = ClientRoot.Instance.Game.AllyProfiles;

        foreach (ClientAllyProfile player in allPlayers)
        {
            var listItem = _listItemScene.Instantiate<ReadyPlayerListItem>();
            bool isReady = readyClients.Contains(player.PeerId);
            
            listItem.SetStatus(player, isReady);
            _playersContainer.AddChild(listItem);
        }
    }
}

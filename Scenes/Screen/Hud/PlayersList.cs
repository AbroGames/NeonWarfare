using Godot;
using System;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class PlayersList : Control
{
    private const string DeadPlayerMark = "\ud83d\udc80";
    [Export] [NotNull] private Label _serverNameLabel { get; set; }
    [Export] [NotNull] private Control _playersListContainer { get; set; }
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public virtual void RebuildPlayersList()
    {
        foreach (var child in _playersListContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        var players = ClientRoot.Instance.Game.World.Allies;
        foreach (var player in players)
        {
            Control item = GetPlayerListItemFor(player);
            _playersListContainer.AddChild(item);
        }
    }
    
    protected virtual Control GetPlayerListItemFor(ClientAlly ally)
    {
        var item = new RichTextLabel();
        item.Text = ally.AllyProfile.Name + (ally.IsDead ? DeadPlayerMark : "");
        item.FitContent = true;
        
        return item;
    }
}

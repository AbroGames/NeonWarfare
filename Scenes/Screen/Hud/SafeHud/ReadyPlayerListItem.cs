using Godot;
using System;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class ReadyPlayerListItem : MarginContainer
{
    private const string ReadyPrefix = "\u2705";
    private readonly Color _readyColor = new Color(1f, 1f, 1f);
    private readonly Color _readyBgColor = new Color(0.0f, 0.2f, 0.0f);
    
    private const string NotReadyPrefix = "\u274c";
    private readonly Color _notReadyColor = new Color(0.7f, 0.7f, 0.7f);
    private readonly Color _notReadyBgColor = new Color(0.1f, 0.0f, 0.0f);
    
    [Export] [NotNull] private Label _nameLabel { get; set; }
    [Export] [NotNull] private ColorRect _nameLabelBackgroundRect { get; set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void SetStatus(ClientAllyProfile playerProfile, bool ready)
    {
        _nameLabel.Text = $"{playerProfile.Name}: {(ready ? ReadyPrefix : NotReadyPrefix)}";
        _nameLabel.LabelSettings.FontColor = ready ? _readyColor : _notReadyColor;
        _nameLabelBackgroundRect.Color = ready ? _readyBgColor : _notReadyBgColor;
    }
}

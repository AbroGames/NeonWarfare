using Godot;
using System;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class DisconnectedScreen : Control
{
    [Export] [NotNull] private Label _disconnectMessageLabel { get; set; }
    [Export] [NotNull] private Label _disconnectReasonDescriptionLabel { get; set; }
    [Export] [NotNull] private Button _backToMenuButton { get; set; }
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);

        _backToMenuButton.Pressed += () =>
        {
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.MainMenu);
        };
    }

    public void InitializeFrom(ClientGame.SC_DisconnectedFromServerPacket packet)
    {
        _disconnectMessageLabel.Text = packet.Message;
        _disconnectReasonDescriptionLabel.Text = packet.ReasonDescription;
    }
}

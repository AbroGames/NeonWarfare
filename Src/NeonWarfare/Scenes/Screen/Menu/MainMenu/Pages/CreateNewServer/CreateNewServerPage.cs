using System;
using Godot;
using Humanizer;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Worlds.Features.Options;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.CreateNewServer;

public partial class CreateNewServerPage : MainMenuPage
{
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public LineEdit SaveNameLineEdit { get; private set; }
    [Child] public CheckBox AllBuildingsUnlockedCheckBox { get; private set; }
    [Child] public SpinBox FriendlyFireSpinBox { get; private set; }
    [Child] public CheckButton IsDedicatedCheckButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        CreateServerButton.Pressed += ParseAndStartServer;
        CancelButton.Pressed += () => GoBack();
        PortSpinBox.Value = Consts.DefaultPort;
        SaveNameLineEdit.Text = Services.SaveLoad.GenNewSaveFileName().Value;
        IsDedicatedCheckButton.ButtonPressed = false;
    }

    private void ParseAndStartServer()
    {
        int port = (int) PortSpinBox.Value;
        SaveFileName saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
            ? Services.SaveLoad.GenNewSaveFileName()
            : new SaveFileName(SaveNameLineEdit.Text.Trim());
        if (!saveFileName.IsValid)
        {
            GoNext(PagesProvider.PrepareMessagePage(
                Tr("MESSAGE_MENU__INVALID_SAVE_NAME").FormatWith(SaveFileName.MaxLength)));
            return;
        }
        bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
        WorldOptions options = NewWorldOptionsForm.Read(AllBuildingsUnlockedCheckBox, FriendlyFireSpinBox);
        TryStartGame(() =>
            Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated, showWindow: true, options));
    }
}

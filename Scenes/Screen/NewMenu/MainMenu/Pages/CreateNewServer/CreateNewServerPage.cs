using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.CreateNewServer;

public partial class CreateNewServerPage : MainMenuPage
{
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public LineEdit SaveNameLineEdit { get; private set; }
    [Child] public CheckButton IsDedicatedCheckButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        CreateServerButton.Pressed += ParseAndStartServer;
        CancelButton.Pressed += () => GoBack();
        PortSpinBox.Value = Consts.DefaultPort;
        SaveNameLineEdit.Text = Services.SaveLoad.GenNewSaveFileName();
        IsDedicatedCheckButton.ButtonPressed = false;
    }

    private void ParseAndStartServer()
    {
        int port = (int) PortSpinBox.Value;
        string saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
            ? Services.SaveLoad.GenNewSaveFileName()
            : SaveNameLineEdit.Text.Trim();
        bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
        TryStartGame(() => Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated));
    }
}

using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Host;

public partial class HostPage : MainMenuPage
{
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public TextEdit SaveNameTextEdit { get; private set; }
    [Child] public CheckButton IsDedicatedCheckButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        CreateServerButton.Pressed += ParseAndStartServer;
        CancelButton.Pressed += () => GoBack();
        PortSpinBox.Value = Consts.DefaultPort;
        SaveNameTextEdit.Text = String.Empty;
        IsDedicatedCheckButton.ButtonPressed = false;
    }
    
    private void ParseAndStartServer()
    {
        int port = (int) PortSpinBox.Value;
        string saveFileName = String.IsNullOrWhiteSpace(SaveNameTextEdit.Text)
            ? Services.SaveLoad.GenNewSaveFileName()
            : SaveNameTextEdit.Text.Trim();
        bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
        Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated);
    }
}
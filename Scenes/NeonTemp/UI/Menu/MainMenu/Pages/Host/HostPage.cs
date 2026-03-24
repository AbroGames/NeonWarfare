using Godot;
using System;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using Kludgeful.Main.SettingsSystem;
using NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu;
using Serilog;

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

        PortSpinBox.Value = Services.GameSettings.Settings.LastHostedPort;
        SaveNameTextEdit.Text = Services.GameSettings.Settings.LastHostedSaveName;
        IsDedicatedCheckButton.ButtonPressed = Services.GameSettings.Settings.LastHostedIsDedicated;
    }
    
    private void ParseAndStartServer()
    {
        int port = (int)PortSpinBox.Value;
        string saveFileName = SaveNameTextEdit.Text.Length != 0 ? SaveNameTextEdit.Text : null;
        bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
        Services.GameSettings.PreserveServerCreation(port, saveFileName, isDedicated);
        Services.MainScene.HostMultiplayerGameAsClient(port, saveFileName, isDedicated);
    }
}

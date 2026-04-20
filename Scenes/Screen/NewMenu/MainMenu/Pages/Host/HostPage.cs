using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scripts.Service.Settings;

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
        //TODO: Аналогично комменту из страницы коннекта.
        // Было:
        // PortSpinBox.Value = Services.GameSettings.GetSettings().LastGame.Port ?? Consts.DefaultPort;
        // SaveNameTextEdit.Text = Services.GameSettings.GetSettings().LastGame.Host ?? String.Empty;
        // IsDedicatedCheckButton.ButtonPressed = Services.GameSettings.GetSettings().LastGame.IsDedicated ?? false;
        PortSpinBox.Value = Consts.DefaultPort;
        SaveNameTextEdit.Text = String.Empty;
        IsDedicatedCheckButton.ButtonPressed = false;
    }
    
    private void ParseAndStartServer()
    {
        int port = (int) PortSpinBox.Value;
        string saveFileName = SaveNameTextEdit.Text.Length != 0 ? SaveNameTextEdit.Text : null;
        bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
        Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated);
    }
}
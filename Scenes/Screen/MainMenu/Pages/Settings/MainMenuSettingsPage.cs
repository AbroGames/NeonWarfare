using Godot;
using NeonWarfare.Scripts.Service.Settings;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Screen.MainMenu.Pages.Settings;

public partial class MainMenuSettingsPage : MainMenuPage
{
    
    [Child] public TextEdit NickTextEdit { get; private set; }
    [Child] public TextEdit ColorTextEdit { get; private set; }
    [Child] public Button SaveReturnButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        PlayerSettings playerSettings = Services.PlayerSettings.GetPlayerSettings();
        NickTextEdit.Text = playerSettings.Nick;
        ColorTextEdit.Text = playerSettings.Color.ToHtml(false);

        SaveReturnButton.Pressed += ParseAndSaveSettings;
    }

    private void ParseAndSaveSettings()
    {
        string nick = NickTextEdit.Text;
        Color color = Color.FromHtml(ColorTextEdit.Text);
        
        Services.PlayerSettings.SetPlayerSettings(new PlayerSettings(nick, color));
        ChangeMenuPage(PackedScenes.Main);
    }
}
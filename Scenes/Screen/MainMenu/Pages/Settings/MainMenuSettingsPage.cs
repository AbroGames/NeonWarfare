using Godot;
using NeonWarfare.Scripts.Service.Settings;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.Godot.Services;

namespace NeonWarfare.Scenes.Screen.MainMenu.Pages.Settings;

public partial class MainMenuSettingsPage : MainMenuPage
{
    
    [Child] public TextEdit NickTextEdit { get; private set; }
    [Child] public TextEdit ColorTextEdit { get; private set; }
    [Child] public OptionButton LanguageOptionButton { get; private set; }
    [Child] public Button SaveReturnButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        PlayerSettings playerSettings = Services.PlayerSettings.GetPlayerSettings();
        NickTextEdit.Text = playerSettings.Nick;
        ColorTextEdit.Text = playerSettings.Color.ToHtml(false);
        LanguageOptionButton.Text = Services.I18N.GetLocaleInfoByCode(playerSettings.Language).Name;
        
        foreach (I18NService.LocaleInfo localeInfo in Services.I18N.Locales)
        {
            LanguageOptionButton.GetPopup().AddItem(localeInfo.Name);
        }
        
        LanguageOptionButton.ItemSelected += _ => Services.I18N.SetCurrentLocale(GetLocaleCodeFromOptionButton());
        SaveReturnButton.Pressed += ParseAndSaveSettings;
    }

    private void ParseAndSaveSettings()
    {
        string nick = NickTextEdit.Text;
        Color color = Color.FromHtml(ColorTextEdit.Text);
        string locale = GetLocaleCodeFromOptionButton();
        
        Services.PlayerSettings.SetPlayerSettings(new PlayerSettings(nick, color, locale));
        Services.I18N.SetCurrentLocale(locale);
        ChangeMenuPage(PackedScenes.Main);
    }

    private string GetLocaleCodeFromOptionButton()
    {
        return Services.I18N.GetLocaleInfoByName(LanguageOptionButton.Text).Code;
    }
}
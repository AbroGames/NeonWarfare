using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class LanguageSelectionPage : MainMenuPage
{
    [Child] public Button SaveButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }
    [Child] public VBoxContainer LanguagesListContainer { get; private set; }

    private ButtonGroup _buttonGroup = new();
    private string _initialLanguage;
    public override void _Ready()
    {
        Di.Process(this);
        
        SaveButton.Pressed += OnSave;
        CancelButton.Pressed += OnCancel;
        
        _initialLanguage = Services.GameSettings.GetSettings().Locale;
        PopulateLanguagesList();
    }

    private void PopulateLanguagesList()
    {
        foreach (var locale in Services.I18N.Locales)
        {
            var radioButton = new CheckBox();
            radioButton.ButtonGroup = _buttonGroup;
            radioButton.Text = $"{locale.Name} ({locale.Code})";
            radioButton.Pressed += () => ApplyLanguage(locale.Code);
            if (locale.Code == _initialLanguage)
            {
                radioButton.ButtonPressed = true;
            }
            LanguagesListContainer.AddChild(radioButton);
        }
    }
    
    private void OnSave()
    {
        Services.GameSettings.SetSettings(
                Services.GameSettings.GetSettings() with
                {
                    Locale = Services.I18N.GetCurrentLocaleInfo().Code
                });
        
        GoBack();
    }

    private void OnCancel()
    {
        ApplyLanguage(_initialLanguage);
        GoBack();
    }

    private void ApplyLanguage(string code)
    {
        Services.I18N.SetCurrentLocale(code);
    }
}

using Godot;
using KludgeBox.Godot.Services;
using NeonWarfare.Scripts;

namespace NeonWarfare.GameTests.Scripts.GlobalServices.Settings.Fixtures;

/// <summary>
/// Initializes <c>Services.I18N</c> over an empty base translation, since the test project registers none of the
/// game's.
/// The base locale is then the only one, so it is the default whatever the OS language.
/// </summary>
public static class TestLocales
{
    public static void Load()
    {
        if (Services.I18N.Locales != null) return;

        TranslationServer.AddTranslation(new Translation { Locale = I18NService.DefaultLocale });
        Services.I18N.Init((SceneTree) Engine.GetMainLoop());
    }
}

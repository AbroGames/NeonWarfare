namespace NeonWarfare.Scripts.GlobalServices.Settings;

public record DedicatedServerSettings(
    string Locale,
    bool AutoSaveEnabled
)
{
    public static DedicatedServerSettings GetDefault()
    {
        return new(
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code,
            AutoSaveEnabled: true
        );
    }
}

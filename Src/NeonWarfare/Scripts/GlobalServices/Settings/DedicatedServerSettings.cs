namespace NeonWarfare.Scripts.GlobalServices.Settings;

public record DedicatedServerSettings(
    string Locale,
    int AutoSaveIntervalMinutes
)
{
    public static DedicatedServerSettings GetDefault()
    {
        return new(
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code,
            AutoSaveIntervalMinutes: 10
        );
    }
}

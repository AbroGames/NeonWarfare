using KludgeBox.Logging;
using KludgeBox.Persistence;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices.Settings;

public class DedicatedServerSettingsService
{
    private const string DefaultFileName = "dedicated-server-settings.json";

    private readonly ILogger _log = LogFactory.GetForStatic<DedicatedServerSettingsService>();

    private DedicatedServerSettings _settings;

    /// <summary>
    /// A setting that is missing or bad in the file gets its default, written back, and the other settings stay.
    /// An unknown locale is bad too.
    /// </summary>
    /// <param name="fileName"><c>null</c> — <c>dedicated-server-settings.json</c>.</param>
    public void Init(string fileName)
    {
        var file = new JsonFile<DedicatedServerSettings>(fileName ?? DefaultFileName);
        _settings = file.ReadOverDefault(DedicatedServerSettings.GetDefault);
        // A known locale in another spelling, like "EN_us", is replaced with its code too
        string locale = (Services.I18N.GetLocaleInfoByCode(_settings.Locale)
            ?? Services.I18N.GetUserOsLocaleInfoOrDefault()).Code;
        if (locale != _settings.Locale)
        {
            _log.Warning("The locale '{locale}' in the settings is replaced with '{newLocale}'",
                _settings.Locale, locale);
            _settings = _settings with { Locale = locale };
            file.Write(_settings);
        }
    }

    public DedicatedServerSettings GetSettings()
    {
        return _settings;
    }

    /// <returns><c>false</c> when the file could not be written.</returns>
    public bool Write(string fileName, DedicatedServerSettings settings)
    {
        return new JsonFile<DedicatedServerSettings>(fileName).Write(settings);
    }
}

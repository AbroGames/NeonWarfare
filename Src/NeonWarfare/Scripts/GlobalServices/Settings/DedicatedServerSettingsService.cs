using KludgeBox.Logging;
using KludgeBox.Persistence;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices.Settings;

public class DedicatedServerSettingsService
{
    private const string DefaultFileName = "dedicated-server-settings.json";

    private readonly ILogger _log = LogFactory.GetForStatic<DedicatedServerSettingsService>();

    private DedicatedServerSettings _settings;

    /// <summary>
    /// A setting that is missing or bad in the file gets its default, written back, and the other settings stay.
    /// An unknown locale and an autosave interval out of its range are bad too.
    /// </summary>
    /// <param name="fileName"><c>null</c> — <c>dedicated-server-settings.json</c>.</param>
    public void Init(string fileName)
    {
        var file = new JsonFile<DedicatedServerSettings>(fileName ?? DefaultFileName);
        DedicatedServerSettings read = file.ReadOverDefault(DedicatedServerSettings.GetDefault);
        _settings = read with
        {
            // A known locale in another spelling, like "EN_us", is replaced with its code too
            Locale = (Services.I18N.GetLocaleInfoByCode(read.Locale)
                ?? Services.I18N.GetUserOsLocaleInfoOrDefault()).Code,
            AutoSaveIntervalMinutes = read.AutoSaveIntervalMinutes is < 0 or > WorldSaving.MaxAutoSaveIntervalMinutes
                ? DedicatedServerSettings.GetDefault().AutoSaveIntervalMinutes
                : read.AutoSaveIntervalMinutes
        };
        if (_settings != read)
        {
            _log.Warning("Bad settings replaced: " +
                "locale '{locale}' with '{newLocale}', " +
                "autosave interval {interval} with {newInterval}",
                read.Locale, _settings.Locale,
                read.AutoSaveIntervalMinutes, _settings.AutoSaveIntervalMinutes);
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

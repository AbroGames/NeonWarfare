using System;
using System.Text.Json;
using KludgeBox.Logging;
using KludgeBox.Persistence;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices.Settings;

/// <summary>
/// The client settings as they are in the file, so the nick here ignores flag <c>--nick</c>.
/// The player of the session, with the launch overrides on top, is read from <see cref="LocalPlayerService"/>.
/// </summary>
public class GameSettingsService
{
    private const string DefaultFileName = "game-settings.json";

    // Color is a Godot struct whose R/G/B/A are fields, which System.Text.Json skips by default.
    // Without the converter it writes only the computed properties (R8, H, OkHslH, ...), whose
    // setters then overwrite each other on read and produce a wrong color.
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerOptions.Default) { Converters = { new ColorJsonConverter() } };

    private readonly ILogger _log = LogFactory.GetForStatic<GameSettingsService>();
    private readonly JsonFile<GameSettings> _file;

    private GameSettings _settings;

    public GameSettingsService(string fileName = DefaultFileName)
    {
        _file = new JsonFile<GameSettings>(fileName, JsonOptions);
    }

    /// <summary>
    /// A setting that is missing or bad in the file gets its default, written back, and the other settings stay.
    /// A blank nick and an unknown locale are bad too.
    /// </summary>
    public void Init()
    {
        GameSettings read = _file.ReadOverDefault(GameSettings.GetDefault);
        GameSettings repaired = read with
        {
            PlayerNick = string.IsNullOrWhiteSpace(read.PlayerNick)
                ? GameSettings.GetDefault().PlayerNick
                : read.PlayerNick,
            Locale = (Services.I18N.GetLocaleInfoByCode(read.Locale)
                ?? Services.I18N.GetUserOsLocaleInfoOrDefault()).Code
        };
        _settings = read;
        if (repaired != read)
        {
            _log.Warning("Bad settings replaced: nick '{nick}' with '{newNick}', locale '{locale}' with '{newLocale}'",
                read.PlayerNick, repaired.PlayerNick, read.Locale, repaired.Locale);
            SetSettings(repaired);
        }
    }

    public GameSettings GetSettings()
    {
        return _settings;
    }

    /// <summary>Keeps the settings for the session even when the file cannot be written.</summary>
    public void SetSettings(GameSettings gameSettings)
    {
        ArgumentNullException.ThrowIfNull(gameSettings);
        _settings = gameSettings;
        _file.Write(gameSettings);
    }
}

using KludgeBox.Logging;
using KludgeBox.Persistence;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices;

/// <summary>
/// Owns the uid of this client's player and keeps it in a file of its own, so that a change or a fault of the settings
/// never costs the player their identity.
/// </summary>
public class PlayerUidService
{
    public record StoredPlayerUid(string Value);

    private const string DefaultFileName = "player-uid.json";

    private readonly ILogger _log = LogFactory.GetForStatic<PlayerUidService>();
    private readonly JsonFile<StoredPlayerUid> _file;

    private PlayerUid _uid;

    public PlayerUidService(string fileName = DefaultFileName)
    {
        _file = new JsonFile<StoredPlayerUid>(fileName);
    }

    /// <summary>A missing or corrupt file gets a new uid written in its place.</summary>
    public void Init()
    {
        StoredPlayerUid stored = _file.ReadOrWriteDefault(
            read => PlayerUid.TryParse(read.Value, out _),
            () => new StoredPlayerUid(PlayerUid.Generate().Value));
        _uid = new PlayerUid(stored.Value);
    }

    public PlayerUid Get()
    {
        return _uid;
    }

    /// <summary>An invalid uid is rejected, and the stored one stays.</summary>
    /// <returns><c>false</c> when the uid is invalid.</returns>
    public bool Set(PlayerUid uid)
    {
        if (!uid.IsValid)
        {
            _log.Warning("Invalid player uid '{uid}' is not stored", uid);
            return false;
        }
        _uid = uid;
        _file.Write(new StoredPlayerUid(uid.Value));
        return true;
    }
}

using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.GlobalServices.Settings;

namespace NeonWarfare.Scripts.GlobalServices;

/// <summary>
/// Who this client process plays as: the uid of <see cref="PlayerUidService"/> and the nick and color of the settings,
/// with the temporary <c>--nick</c> / <c>--uid</c> on top. The overrides live only here, so they never reach a file.
/// </summary>
public class LocalPlayerService
{
    private string _nickOverride;
    private PlayerUid? _uidOverride;

    /// <param name="nick"><c>null</c> — the stored nick.</param>
    /// <param name="uid"><c>null</c> — the stored uid.</param>
    public void SetOverrides(string nick, PlayerUid? uid)
    {
        _nickOverride = nick;
        _uidOverride = uid;
    }

    public LocalPlayer Read()
    {
        GameSettings stored = Services.GameSettings.GetSettings();
        return new LocalPlayer(
            _uidOverride ?? Services.PlayerUid.Get(),
            _nickOverride ?? stored.PlayerNick,
            stored.PlayerColor);
    }
}

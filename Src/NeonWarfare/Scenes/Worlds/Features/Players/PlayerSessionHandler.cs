using System.Globalization;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[CommandHandler]
public class PlayerSessionHandler(PlayerSimulationFacade playerSimulationFacade) : IPeerSessionHandler
{
    // The format of the client's uid generator, two groups of Latin letters joined by a dash
    private const int UidGroupLength = 10;
    private const char UidSeparator = '-';
    private const int NickMinLength = 3;
    private const int NickMaxLength = 25;
    // Darker colors are lost on the dark background of the game
    private const float ColorMinLuminance = 0.2f;

    public bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason)
    {
        if (!IsValidUid(command.Uid))
        {
            reason = JoinRejectReason.InvalidUid;
            return false;
        }
        if (!IsValidNick(command.Nick))
        {
            reason = JoinRejectReason.InvalidNick;
            return false;
        }
        if (!IsValidColor(command.Color))
        {
            reason = JoinRejectReason.InvalidColor;
            return false;
        }

        reason = default;
        return true;
    }

    public void Join(JoinRequestCommand command) =>
        playerSimulationFacade.Join(command.Uid, command.Nick, command.Color);

    public void Leave(string uid) => playerSimulationFacade.Leave(uid);

    private static bool IsValidUid(string uid)
    {
        return uid is { Length: UidGroupLength * 2 + 1 }
               && uid[UidGroupLength] == UidSeparator
               && uid.Where((_, i) => i != UidGroupLength).All(char.IsAsciiLetter);
    }

    private static bool IsValidNick(string nick)
    {
        return nick is { Length: >= NickMinLength and <= NickMaxLength }
               && !nick.Any(c => char.IsWhiteSpace(c) || IsForbiddenChar(c));
    }

    private static bool IsValidColor(Color color)
    {
        float[] components = [color.R, color.G, color.B, color.A];
        return components.All(component => component is >= 0 and <= 1) && color.Luminance >= ColorMinLuminance;
    }

    private static bool IsForbiddenChar(char c)
    {
        return char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
    }
}

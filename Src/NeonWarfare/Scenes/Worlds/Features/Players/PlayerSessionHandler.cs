using System.Linq;
using Godot;
using KludgeBox.Core.Text;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[CommandHandler]
public class PlayerSessionHandler(PlayerSimulationFacade playerSimulationFacade) : IPeerSessionHandler
{
    private const int NickMinLength = 3;
    private const int NickMaxLength = 25;
    // A darker color is lost on the dark background of the game.
    private const float ColorMinLuminance = 0.2f;

    public bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason)
    {
        if (!command.Uid.IsValid)
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

    public void Leave(PlayerUid uid, LeaveReason reason) => playerSimulationFacade.Leave(uid, reason);

    private static bool IsValidNick(string nick)
    {
        return nick is { Length: >= NickMinLength and <= NickMaxLength } && nick.IsSingleWordPrintable();
    }

    private static bool IsValidColor(Color color)
    {
        float[] components = [color.R, color.G, color.B, color.A];
        return components.All(component => component is >= 0 and <= 1) && color.Luminance >= ColorMinLuminance;
    }
}

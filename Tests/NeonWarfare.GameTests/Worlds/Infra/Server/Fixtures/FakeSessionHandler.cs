using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures;

// Records whether the peer is bound and has its buffer by the time of Join; a uid picks the failure
public class FakeSessionHandler(List<string> calls, PeerUidMap peers, EventOutbox outbox) : IPeerSessionHandler
{
    public const string Invalid = "invalid";
    public const string ThrowInValidate = "throw in validate";
    public const string ThrowInJoin = "throw in join";
    public const string ThrowInLeave = "throw in leave";

    public static string Joined(string uid) => $"join {uid}: bound, buffer";

    public bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason)
    {
        calls.Add($"validate join {command.Uid}");
        reason = JoinRejectReason.InvalidNick;
        return command.Uid switch
        {
            ThrowInValidate => throw new InvalidOperationException("validate failed"),
            Invalid => false,
            _ => true,
        };
    }

    public void Join(JoinRequestCommand command)
    {
        bool bound = peers.TryGetPeerId(command.Uid, out int peerId);
        string buffer = bound && outbox.Peers.Contains(peerId) ? "buffer" : "no buffer";
        calls.Add($"join {command.Uid}: {(bound ? "bound" : "not bound")}, {buffer}");
        if (command.Uid == ThrowInJoin) throw new InvalidOperationException("join failed");
    }

    public void Leave(string uid)
    {
        calls.Add($"leave {uid}");
        if (uid == ThrowInLeave) throw new InvalidOperationException("leave failed");
    }
}

using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

namespace NeonWarfare.GameTests.Worlds.Infra.ServerNetwork.Fixtures;

// Records whether the peer is bound and has its buffer by the time of Process; a uid picks the failure
public class FakeJoinHandler(List<string> calls, PeerUidMap peers, EventOutbox outbox)
    : IJoinRequestHandler, IPeerDisconnectedHandler
{
    public const string Invalid = "invalid";
    public const string ThrowInValidate = "throw in validate";
    public const string ThrowInProcess = "throw in process";
    public const string ThrowInLeave = "throw in leave";

    public static string Processed(string uid) => $"process join {uid}: bound, buffer";

    public bool Validate(JoinRequestCommand command, out JoinRejectReason reason)
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

    public void Process(JoinRequestCommand command)
    {
        bool bound = peers.TryGetPeerId(command.Uid, out int peerId);
        string buffer = bound && outbox.Peers.Contains(peerId) ? "buffer" : "no buffer";
        calls.Add($"process join {command.Uid}: {(bound ? "bound" : "not bound")}, {buffer}");
        if (command.Uid == ThrowInProcess) throw new InvalidOperationException("process failed");
    }

    public void Process(string uid)
    {
        calls.Add($"leave {uid}");
        if (uid == ThrowInLeave) throw new InvalidOperationException("leave failed");
    }
}

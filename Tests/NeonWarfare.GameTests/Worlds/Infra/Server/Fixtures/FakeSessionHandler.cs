using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures;

// Records whether the peer has joined by the time of Join; a uid picks the failure
public class FakeSessionHandler(List<string> calls, PeerStateTable peers) : IPeerSessionHandler
{
    public const string Invalid = "invalid";
    public const string ThrowInValidate = "throw in validate";
    public const string ThrowInJoin = "throw in join";
    public const string ThrowInLeave = "throw in leave";

    public static string Joined(string uid) => $"join {uid}: joined";

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
        bool joined = peers.TryGetJoinedByUid(command.Uid, out _);
        calls.Add($"join {command.Uid}: {(joined ? "joined" : "not joined")}");
        if (command.Uid == ThrowInJoin) throw new InvalidOperationException("join failed");
    }

    public void Leave(string uid)
    {
        calls.Add($"leave {uid}");
        if (uid == ThrowInLeave) throw new InvalidOperationException("leave failed");
    }
}

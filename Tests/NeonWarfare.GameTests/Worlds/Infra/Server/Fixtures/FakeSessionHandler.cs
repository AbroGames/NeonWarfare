using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scripts;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Fixtures;

// Records whether the peer has joined by the time of Join; a uid picks the failure
public class FakeSessionHandler(List<string> calls, PeerStateTable peers) : IPeerSessionHandler
{
    public static readonly PlayerUid Invalid = new("invalid");
    public static readonly PlayerUid ThrowInValidate = new("throw in validate");
    public static readonly PlayerUid ThrowInJoin = new("throw in join");
    public static readonly PlayerUid ThrowInLeave = new("throw in leave");

    public static string Joined(PlayerUid uid) => $"join {uid}: joined";

    public bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason)
    {
        calls.Add($"validate join {command.Uid}");
        reason = JoinRejectReason.InvalidNick;
        if (command.Uid == ThrowInValidate) throw new InvalidOperationException("validate failed");
        return command.Uid != Invalid;
    }

    public void Join(JoinRequestCommand command)
    {
        bool joined = peers.TryGetJoinedByUid(command.Uid, out _);
        calls.Add($"join {command.Uid}: {(joined ? "joined" : "not joined")}");
        if (command.Uid == ThrowInJoin) throw new InvalidOperationException("join failed");
    }

    public static string Displaced(PlayerUid uid) => $"leave {uid}: displaced";

    public void Leave(PlayerUid uid, LeaveReason reason)
    {
        calls.Add(reason == LeaveReason.Displaced ? Displaced(uid) : $"leave {uid}");
        if (uid == ThrowInLeave) throw new InvalidOperationException("leave failed");
    }
}

using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// Handles the join and the leave of a peer. Only <see cref="PeerSessions"/> takes it.
/// Not an <c>IPlayerCommandHandler</c>, because a joining peer has no player yet and a leave is not a command.
/// Deliberately not generic, so no command can bypass the rule that its sender is a joined player.
/// </summary>
public interface IPeerSessionHandler
{
    /// <summary>
    /// Checks the fields the client fills, because the body is untrusted input.
    /// A <c>false</c> result rejects the peer with <c>JoinRejected</c>, so the client does not wait for the handshake
    /// timeout.
    /// </summary>
    bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason);
    void Join(JoinRequestCommand command);

    /// <summary>
    /// Called once for a joined peer, when it disconnects or when another peer displaces it.
    /// </summary>
    void Leave(PlayerUid uid, LeaveReason reason);
}

public enum LeaveReason
{
    Disconnected,
    /// <summary>The same uid joins from another peer right after this leave, so the player is back, not gone.</summary>
    Displaced
}

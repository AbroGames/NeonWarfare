using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;


namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

// Not an IPlayerCommandHandler: the joining peer has no player yet, and a leave is no command. Not generic either:
// a peer-level handler for any command would let commands bypass "the sender is a joined player".
// ValidateJoin checks the fields a client fills (the body comes from untrusted input); a false result rejects the
// peer with JoinRejected, since a silent drop would leave the client waiting for the handshake timeout.
// Leave is called once for a joined peer, when it disconnects or when another peer displaces it.
public interface IPeerSessionHandler : ICommandHandler
{
    bool ValidateJoin(JoinRequestCommand command, out JoinRejectReason reason);
    void Join(JoinRequestCommand command);
    void Leave(string uid);
}

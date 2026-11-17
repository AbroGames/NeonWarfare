using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;


namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

// Not an IPlayerCommandHandler: the joining peer has no player yet. Not generic either: a peer-level handler
// for any command would let commands bypass "the sender is a joined player". Validate checks the fields a client
// fills (the body comes from untrusted input); a false result rejects the peer with JoinRejected, since a silent
// drop would leave the client waiting for the handshake timeout.
// It is the pair for IPeerDisconnectedHandler.
public interface IJoinRequestHandler
{
    bool Validate(JoinRequestCommand command, out JoinRejectReason reason);
    void Process(JoinRequestCommand command);
}

using NeonWarfare.Scenes.World.Commands;

namespace NeonWarfare.Scenes.World.CommandHandlers;

// Not an IPlayerCommandHandler: the joining peer has no PlayerModel yet. Not generic either: a peer-level handler
// for any command would let commands bypass "the sender is a joined player". Validate checks the fields a client
// fills (the body comes from untrusted input); a false result rejects the peer with JoinRejected, since a silent
// drop would leave the client waiting for the handshake timeout.
public interface IJoinRequestHandler
{
    bool Validate(int peerId, JoinRequestCommand command, out string reason);
    void Process(int peerId, JoinRequestCommand command);
}

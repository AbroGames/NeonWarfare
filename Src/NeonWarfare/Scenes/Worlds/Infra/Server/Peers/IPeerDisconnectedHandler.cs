
namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

// The pair of IJoinRequestHandler: called for a joined peer only, both when it disconnects and when another peer
// displaces it, before its uid is unbound.
public interface IPeerDisconnectedHandler
{
    void Process(string uid);
}

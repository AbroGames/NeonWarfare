using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.GameTests.Game.Fixtures;

/// <summary>
/// Plays the World for a transport: records every call into one list, so a test sees the order across the roles.
/// </summary>
public class RecordingWorldEndpoint : World.IServerEndpoint, World.IClientEndpoint
{
    public record Call(string Name, int? PeerId = null, byte[]? Body = null);

    public List<Call> Calls { get; } = [];

    // What a World does with a body it cannot read or apply
    public bool ThrowOnReceive { get; set; }

    public void StartHandshake(int peerId) => Calls.Add(new Call(nameof(StartHandshake), peerId));

    public void QueueDisconnection(int peerId) => Calls.Add(new Call(nameof(QueueDisconnection), peerId));

    public void ReceiveFromClient(int peerId, ReadOnlyMemory<byte> packet) =>
        Calls.Add(new Call(nameof(ReceiveFromClient), peerId, packet.ToArray()));

    public void ReceiveState(ReadOnlyMemory<byte> body) => Receive(nameof(ReceiveState), body);

    public void ReceiveEvents(ReadOnlyMemory<byte> body) => Receive(nameof(ReceiveEvents), body);

    private void Receive(string name, ReadOnlyMemory<byte> body)
    {
        Calls.Add(new Call(name, Body: body.ToArray()));
        if (ThrowOnReceive) throw new NetMessageFormatException("Broken body");
    }
}

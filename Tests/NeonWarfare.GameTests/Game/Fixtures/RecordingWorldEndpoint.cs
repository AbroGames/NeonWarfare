using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.GameTests.Game.Fixtures;

/// <summary>
/// Plays the World for a transport: records every call into one list, so a test sees the order across the roles.
/// </summary>
public class RecordingWorldEndpoint : World.IServerEndpoint, World.IHostEndpoint, World.IClientEndpoint
{
    // A tick update is recorded with its tick, its state section as the body and its events section
    public record Call(string Name, int? PeerId = null, byte[]? Body = null, long? Tick = null, byte[]? Events = null);

    public List<Call> Calls { get; } = [];

    // What a World does with a body it cannot read or apply
    public bool ThrowOnReceive { get; set; }

    public void StartHandshake(int peerId) => Calls.Add(new Call(nameof(StartHandshake), peerId));

    public void QueueDisconnection(int peerId) => Calls.Add(new Call(nameof(QueueDisconnection), peerId));

    public void ReceiveFromClient(int peerId, ReadOnlyMemory<byte> packet) =>
        Calls.Add(new Call(nameof(ReceiveFromClient), peerId, packet.ToArray()));

    public void ReceiveOwnEvents(ReadOnlyMemory<byte> events)
    {
        Calls.Add(new Call(nameof(ReceiveOwnEvents), Events: events.ToArray()));
        if (ThrowOnReceive) throw new NetMessageFormatException("Broken section");
    }

    public void ReceiveTickUpdate(long tick, ReadOnlyMemory<byte> state, ReadOnlyMemory<byte> events)
    {
        Calls.Add(new Call(nameof(ReceiveTickUpdate), Body: state.ToArray(), Tick: tick, Events: events.ToArray()));
        if (ThrowOnReceive) throw new NetMessageFormatException("Broken section");
    }
}

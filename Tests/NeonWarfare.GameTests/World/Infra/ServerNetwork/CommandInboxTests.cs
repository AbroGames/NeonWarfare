using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork;

[TestSuite]
public class CommandInboxTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    // JoinRequestCommand is left out on purpose: a command type without a handler must be rejected too
    private static readonly HashSet<Type> NetworkCommandTypes = [typeof(SendChatMessageCommand)];

    private NetMessageCodec _codec = null!;
    private RecordingClientsConnection _clientsConnection = null!;
    private PeerGatekeeper _gatekeeper = null!;
    private CommandInbox _inbox = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _clientsConnection = new RecordingClientsConnection();
        _gatekeeper = new PeerGatekeeper(_clientsConnection, new ManualTimeProvider(0), new PeerUidMap());
        _inbox = new CommandInbox(_codec, _gatekeeper);
        _inbox.Register(NetworkCommandTypes);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EnqueueFromPeer_AllowedCommand_ComesOutDecodedWithItsPeer()
    {
        var command = new SendChatMessageCommand("hi");

        _inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(command));

        AssertThat(_inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommand(AlicePeer, command));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EnqueueFromPeer_RejectedPacket_EnqueuesNothingAndTheNextIsAccepted()
    {
        byte[] chat = _codec.Encode(new SendChatMessageCommand("hi"));
        byte[][] rejected =
        [
            _codec.Encode(new ChatServerMessageEvent(1, "an event")),
            _codec.Encode(new JoinRequestCommand(_codec.ProtocolHash, "uid", "nick", Colors.Red)),
            chat[..^1],
            [..chat, 0],
        ];
        var next = new SendChatMessageCommand("next");

        foreach (byte[] packet in rejected)
        {
            _inbox.EnqueueFromPeer(AlicePeer, packet);
        }
        _inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(next));

        AssertThat(_inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommand(AlicePeer, next));
    }

    // A client of another build: rejected before its join can reach the world, and told why
    [TestCase]
    [RequireGodotRuntime]
    public void EnqueueFromPeer_JoinWithAnotherProtocolHash_IsRejectedAndNotEnqueued()
    {
        CommandInbox inbox = JoinInbox();
        var join = new JoinRequestCommand(_codec.ProtocolHash, "alice", "Alice", Colors.Red);

        inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(join with { ProtocolHash = _codec.ProtocolHash + 1 }));
        inbox.EnqueueFromPeer(BobPeer, _codec.Encode(join));

        AssertThat(inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommand(BobPeer, join));
        AssertThat(_clientsConnection.Packets.Count).IsEqual(1);
        AssertThat(_clientsConnection.Packets[0].PeerId).IsEqual(AlicePeer);
        AssertThat(_clientsConnection.Packets[0].Packet)
            .ContainsExactly((byte) ServerPacketKind.JoinRejected, (byte) JoinRejectReason.ProtocolMismatch);
        AssertThat(_clientsConnection.Disconnected).ContainsExactly(AlicePeer);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TakeAll_ReturnsEveryKindInArrivalOrder()
    {
        var command = new SendChatMessageCommand("peer");

        _inbox.EnqueuePeerDisconnected(BobPeer);
        _inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(command));
        _inbox.EnqueuePeerDisconnected(AlicePeer);

        AssertThat(_inbox.TakeAll()).ContainsExactly(
            new CommandInbox.PeerDisconnected(BobPeer),
            new CommandInbox.PeerCommand(AlicePeer, command),
            new CommandInbox.PeerDisconnected(AlicePeer));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TakeAll_EntryEnqueuedAfterIt_WaitsForTheNextCall()
    {
        _inbox.EnqueuePeerDisconnected(AlicePeer);
        IReadOnlyList<CommandInbox.Entry> taken = _inbox.TakeAll();

        _inbox.EnqueuePeerDisconnected(BobPeer);

        AssertThat(taken).ContainsExactly(new CommandInbox.PeerDisconnected(AlicePeer));
        AssertThat(_inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerDisconnected(BobPeer));
        AssertThat(_inbox.TakeAll()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_Twice_Throws()
    {
        AssertThrown(() => _inbox.Register(NetworkCommandTypes)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EnqueueFromPeer_BeforeRegister_Throws()
    {
        var inbox = new CommandInbox(_codec, _gatekeeper);
        byte[] packet = _codec.Encode(new SendChatMessageCommand("hi"));

        AssertThrown(() => inbox.EnqueueFromPeer(AlicePeer, packet)).IsInstanceOf<InvalidOperationException>();
    }

    private CommandInbox JoinInbox()
    {
        var inbox = new CommandInbox(_codec, _gatekeeper);
        inbox.Register(new HashSet<Type> { typeof(JoinRequestCommand) });
        return inbox;
    }
}

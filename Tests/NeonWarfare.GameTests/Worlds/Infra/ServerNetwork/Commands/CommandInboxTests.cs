using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.ServerNetwork.Commands;

[TestSuite]
public class CommandInboxTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

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
        // No join handler on purpose: a command type without a handler must be rejected too
        _inbox = Inbox(new ChatHandler());
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EnqueueFromPeer_AllowedCommand_ComesOutDecodedWithItsPeer()
    {
        var command = new SendChatMessageCommand("hi");

        _inbox.EnqueueFromPeer(AlicePeer, _codec.Encode(command));

        AssertThat(_inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommandEntry(AlicePeer, command));
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

        AssertThat(_inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommandEntry(AlicePeer, next));
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

        AssertThat(inbox.TakeAll()).ContainsExactly(new CommandInbox.PeerCommandEntry(BobPeer, join));
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
            new CommandInbox.PeerCommandEntry(AlicePeer, command),
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
    public void EnqueueFromPeer_BeforeRegister_Throws()
    {
        var inbox = new CommandInbox(_codec, _gatekeeper, new CommandHandlerRegistry());
        byte[] packet = _codec.Encode(new SendChatMessageCommand("hi"));

        AssertThrown(() => inbox.EnqueueFromPeer(AlicePeer, packet)).IsInstanceOf<InvalidOperationException>();
    }

    private CommandInbox JoinInbox() => Inbox(new JoinLeaveHandler());

    private CommandInbox Inbox(params object[] handlers)
    {
        var registry = new CommandHandlerRegistry();
        registry.Register(handlers);
        return new CommandInbox(_codec, _gatekeeper, registry);
    }

    private class ChatHandler : IPlayerCommandHandler<SendChatMessageCommand>
    {
        public bool Validate(string senderUid, SendChatMessageCommand command) => true;

        public void Process(string senderUid, SendChatMessageCommand command) { }
    }

    private class JoinLeaveHandler : IJoinRequestHandler, IPeerDisconnectedHandler
    {
        public bool Validate(JoinRequestCommand command, out JoinRejectReason reason)
        {
            reason = default;
            return true;
        }

        public void Process(JoinRequestCommand command) { }

        public void Process(string uid) { }
    }
}

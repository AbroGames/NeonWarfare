using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Protocol;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.ServerNetwork;

[TestSuite]
public class CommandInboxTests
{
    private const int AlicePeer = 2;
    private const int BobPeer = 3;

    // JoinRequestCommand is left out on purpose: a command type without a handler must be rejected too
    private static readonly HashSet<Type> NetworkCommandTypes = [typeof(SendChatMessageCommand)];

    private NetMessageCodec _codec = null!;
    private CommandInbox _inbox = null!;

    [BeforeTest]
    public void SetUp()
    {
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        _inbox = new CommandInbox(_codec);
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
            _codec.Encode(new JoinRequestCommand(1, "uid", "nick", Colors.Red)),
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
        var inbox = new CommandInbox(_codec);
        byte[] packet = _codec.Encode(new SendChatMessageCommand("hi"));

        AssertThrown(() => inbox.EnqueueFromPeer(AlicePeer, packet)).IsInstanceOf<InvalidOperationException>();
    }
}

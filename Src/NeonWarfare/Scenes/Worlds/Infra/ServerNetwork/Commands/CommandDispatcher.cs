using System;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;

/// <summary>
/// Drains <see cref="CommandInbox"/> in the server tick and hands every entry to its handler: a join and a
/// disconnection to <see cref="PeerSessions"/>, a command to the player handler from
/// <see cref="CommandHandlerRegistry"/>. "Joined or not" is decided here, at tick time rather than on arrival: a join
/// and a chat command of one peer in one tick must both pass, in order. A handler gets the bound uid and looks its
/// own state up by it.
/// </summary>
[ServerNetwork]
public class CommandDispatcher(
    CommandInbox inbox,
    CommandHandlerRegistry handlers,
    PeerSessions sessions,
    PeerUidMap peers,
    PeerGatekeeper gatekeeper)
{
    private const string NotJoinedLog = "{command} from peer {peerId} dropped: the peer has not joined";
    private const string NotValidLog = "{command} from peer {peerId} dropped: {handler} did not validate it";
    private const string DisconnectingLog = "{entry} dropped: the peer is being disconnected";
    private const string EntryFailedLog = "{entry} failed, the rest of the tick goes on";
    private const string NotRegisteredError = "The command handlers are not registered yet.";
    private const string UnknownEntryError = "{0} has no branch in Process.";
    private const string NoPlayerHandlerError =
        "{0} from peer {1} passed the inbox, but there is no player handler: the whitelist is miswired.";

    private readonly ILogger _log = LogFactory.GetForStatic<CommandDispatcher>();

    public void ProcessAll()
    {
        if (!handlers.IsRegistered)
        {
            throw new InvalidOperationException(NotRegisteredError);
        }

        foreach (CommandInbox.Entry entry in inbox.TakeAll())
        {
            // TakeAll has already emptied the inbox, so a throw must not cost the rest of the entries: a lost
            // PeerDisconnected would leave its peer bound. A facade has no rollback, so a throw may leave the model
            // half-changed: the log is the only signal of that
            try
            {
                Process(entry);
            }
            catch (Exception e)
            {
                _log.Error(e, EntryFailedLog, entry);
            }
        }
    }

    private void Process(CommandInbox.Entry entry)
    {
        // Its commands were sent before the disconnect reached it, so they are not its owner's any more
        if (entry is not CommandInbox.PeerDisconnected && gatekeeper.IsDisconnecting(entry.PeerId))
        {
            _log.Debug(DisconnectingLog, entry);
            return;
        }

        switch (entry)
        {
            case CommandInbox.PeerCommandEntry { Command: JoinRequestCommand join }:
                sessions.Join(entry.PeerId, join);
                break;
            case CommandInbox.PeerCommandEntry peerCommand:
                ProcessFromPlayer(peerCommand.PeerId, peerCommand.Command);
                break;
            case CommandInbox.PeerDisconnected:
                sessions.Disconnected(entry.PeerId);
                break;
            default:
                throw new InvalidOperationException(UnknownEntryError.FormatWith(entry.GetType().Name));
        }
    }

    private void ProcessFromPlayer(int peerId, Command command)
    {
        string name = command.GetType().Name;
        if (!peers.TryGetUid(peerId, out string uid))
        {
            _log.Warning(NotJoinedLog, name, peerId);
            return;
        }

        // The inbox lets through only the commands with a handler, so this is a wiring error, not a peer's
        if (!handlers.TryGetPlayerHandler(command.GetType(), out CommandHandlerRegistry.PlayerHandler handler))
        {
            throw new InvalidOperationException(NoPlayerHandlerError.FormatWith(name, peerId));
        }

        if (!handler.Validate(uid, command))
        {
            _log.Warning(NotValidLog, name, peerId, handler.Name);
            return;
        }

        handler.Process(uid, command);
    }
}

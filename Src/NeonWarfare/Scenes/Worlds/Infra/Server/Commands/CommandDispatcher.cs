using System;
using System.Diagnostics;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

/// <summary>
/// Drains <see cref="CommandInbox"/> in the server tick, see Docs/Networking.md#commands-client--server.
/// Whether the sender has joined is decided here, in the tick, not on arrival, so a join and a chat command of one
/// peer in one tick both pass, in order.
/// </summary>
[Server]
public class CommandDispatcher(
    CommandInbox inbox,
    CommandHandlerRegistry handlers,
    PeerSessions sessions,
    PeerStateTable peers)
{
    private readonly ILogger _log = LogFactory.GetForStatic<CommandDispatcher>();

    public void ProcessAll()
    {
        foreach (CommandInbox.Entry entry in inbox.TakeAll())
        {
            // TakeAll has already emptied the inbox, so a throw must not cost the other entries.
            // A lost PeerDisconnected would leave its peer in the table for good.
            // A facade has no rollback, so after a throw the model may stay half-changed, and the log is the only
            // trace of it.
            try
            {
                Process(entry);
            }
            catch (Exception e)
            {
                _log.Error(e, "{entry} failed, the rest of the tick goes on", entry);
            }
        }
    }

    private void Process(CommandInbox.Entry entry)
    {
        // A cut peer sent these before the disconnect reached it, and they no longer belong to its player.
        if (entry is not CommandInbox.PeerDisconnected && peers.IsCut(entry.PeerId))
        {
            _log.Debug("{entry} dropped: the peer is being disconnected", entry);
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
                throw new UnreachableException();
        }
    }

    private void ProcessFromPlayer(int peerId, Command command)
    {
        string name = command.GetType().Name;
        if (!peers.TryGetJoined(peerId, out PeerStateTable.Joined joined))
        {
            _log.Warning("{command} from peer {peerId} dropped: the peer has not joined", name, peerId);
            return;
        }
        PlayerUid uid = joined.Uid;

        CommandHandlerRegistry.PlayerHandler handler = handlers.GetPlayerHandler(command.GetType());
        if (!handler.Validate(uid, command))
        {
            _log.Warning("{command} from peer {peerId} dropped: {handler} did not validate it",
                name, peerId, handler.Name);
            return;
        }

        handler.Process(uid, command);
    }
}

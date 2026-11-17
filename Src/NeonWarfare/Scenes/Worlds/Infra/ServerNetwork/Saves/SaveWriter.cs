using System;
using System.Collections.Generic;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Replication;
using RepliCAT;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Saves;

/// <summary>
/// Writes the save: the protocol hash (<see cref="HashBits"/>), the next NetId and the tick (varuints), then the
/// records of <see cref="StateReplicator.WriteSave"/>. The hash comes first and fixed-size, so a build of any later
/// format can still tell a foreign save before it reads anything else. The save is the state at the end of a tick, the
/// one the joining peers get: a request made during the tick is served after its packets are written.
/// </summary>
[ServerNetwork]
public class SaveWriter(StateReplicator stateReplicator, NetIdGenerator netIds, NetMessageCodec codec)
{
    public const int HashBits = 64;

    private const string WriteFailedLog = "The save failed";
    private const string CallbackFailedLog = "A save callback failed, the other callbacks still run";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveWriter>();

    private readonly List<(Action<byte[]> Written, Action<Exception> Failed)> _requests = [];

    /// <summary>
    /// The save right now. Valid only between ticks, when the baselines are the end of the last one.
    /// </summary>
    /// <exception cref="InvalidOperationException">No tick has been sent since an entity spawned.</exception>
    /// <exception cref="ReplicationException">A manual member cannot be written.</exception>
    public byte[] Write()
    {
        var writer = new BitWriter();
        writer.WriteBits(codec.ProtocolHash, HashBits);
        writer.WriteVarUInt((ulong) netIds.NextValue);
        writer.WriteVarUInt((ulong) stateReplicator.LastSentTick);
        stateReplicator.WriteSave(writer);
        return writer.ToArray();
    }

    /// <summary>
    /// A save at the end of the current tick. The callbacks run inside the tick and must not change the models.
    /// </summary>
    public void RequestSave(Action<byte[]> written, Action<Exception> failed)
    {
        ArgumentNullException.ThrowIfNull(written);
        ArgumentNullException.ThrowIfNull(failed);
        _requests.Add((written, failed));
    }

    // Calls from ServerTickLoop, once the packets of the tick are written. One save for every request of the tick
    public void WriteRequested()
    {
        if (_requests.Count == 0) return;

        var requests = _requests.ToArray();
        _requests.Clear();

        byte[] save;
        try
        {
            save = Write();
        }
        catch (Exception e)
        {
            _log.Error(e, WriteFailedLog);
            foreach ((_, Action<Exception> failed) in requests)
            {
                Run(() => failed(e));
            }
            return;
        }

        foreach ((Action<byte[]> written, _) in requests)
        {
            Run(() => written(save));
        }
    }

    private void Run(Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception e)
        {
            _log.Error(e, CallbackFailedLog);
        }
    }
}

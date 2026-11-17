using Godot;
using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

[MessagePackObject]
public record JoinRequestCommand(
    [property: Key(0)] ulong ProtocolHash,
    [property: Key(1)] string Uid,
    [property: Key(2)] string Nick,
    [property: Key(3)] Color Color) : Command;

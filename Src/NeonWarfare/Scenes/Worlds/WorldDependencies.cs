using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds;

public record WorldDependencies(
    TimeProvider Time,
    NetMessageCodec Codec,
    Replicator Replicator,
    FrameProvider Frames,
    WorldPackedScenes Scenes,
    EntityCatalog Entities,
    IClientsConnection ClientsConnection,
    IServerConnection ServerConnection);

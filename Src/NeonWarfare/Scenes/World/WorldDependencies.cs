using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using RepliCAT;

namespace NeonWarfare.Scenes.World;

public record WorldDependencies(
    TimeProvider Time,
    NetMessageCodec Codec,
    Replicator Replicator,
    FrameProvider Frames,
    WorldPackedScenes Scenes,
    EntityCatalog Entities,
    IClientsConnection ClientsConnection,
    IServerConnection ServerConnection,
    ISaveFiles SaveFiles);

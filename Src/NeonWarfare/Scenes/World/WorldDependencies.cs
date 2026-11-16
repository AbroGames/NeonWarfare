using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using RepliCAT;

namespace NeonWarfare.Scenes.World;

/// <param name="SaveFiles"><c>null</c> on a remote client: it has no save file.</param>
/// <param name="LocalPlayer"><c>null</c> on a dedicated server: it has no player of its own.</param>
/// <param name="Admin"><c>null</c> on a remote client: only the Simulation grants the rights.</param>
/// <param name="DedicatedServerOwner"><c>null</c> except on a dedicated server.</param>
public record WorldDependencies(
    TimeProvider Time,
    NetMessageCodec Codec,
    Replicator Replicator,
    FrameProvider Frames,
    WorldPackedScenes Scenes,
    EntityCatalog Entities,
    IClientsConnection ClientsConnection,
    IServerConnection ServerConnection,
    ISaveFiles SaveFiles,
    LocalPlayer LocalPlayer,
    WorldAdmin Admin,
    IDedicatedServerOwner DedicatedServerOwner);

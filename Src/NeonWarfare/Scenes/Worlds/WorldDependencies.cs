using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.ClientNetwork;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Saves;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds;

/// <param name="SaveFiles"><c>null</c> on a remote client: it has no save file.</param>
/// <param name="LocalPlayer"><c>null</c> on a dedicated server: it has no player of its own.</param>
/// <param name="Admin"><c>null</c> on a remote client: only the Simulation grants the rights.</param>
/// <param name="DedicatedServerOwner"><c>null</c> except on a dedicated server.</param>
/// <param name="LocalPlayerOwner"><c>null</c> exactly when <paramref name="LocalPlayer"/> is.</param>
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
    IDedicatedServerOwner DedicatedServerOwner,
    ILocalPlayerOwner LocalPlayerOwner);

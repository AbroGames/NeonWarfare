using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;

namespace NeonWarfare.Scenes.World;

public record WorldDependencies(
    TimeProvider Time,
    NetMessageCodec Codec,
    FrameProvider Frames,
    WorldPackedScenes Scenes,
    EntityCatalog Entities,
    IClientsConnection ClientsConnection);

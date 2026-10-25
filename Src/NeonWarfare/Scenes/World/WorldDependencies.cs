using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using NeonWarfare.Scenes.World.PackedScenes;

namespace NeonWarfare.Scenes.World;

public record WorldDependencies(
    TimeProvider Time,
    NetMessageCodec Codec,
    FrameProvider Frames,
    WorldPackedScenes Scenes,
    IClientsConnection ClientsConnection);

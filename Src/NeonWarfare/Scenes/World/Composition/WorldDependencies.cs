using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scenes.World.ServerNetwork;

namespace NeonWarfare.Scenes.World.Composition;

public record WorldDependencies(
    TimeProvider Time,
    PersistenceModel Persistence,
    SessionModel Session,
    NetMessageCodec Codec,
    FrameProvider Frames,
    WorldPackedScenes Scenes,
    IClientsConnection ClientsConnection);

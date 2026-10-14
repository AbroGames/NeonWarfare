using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;

namespace NeonWarfare.Scenes.World.Composition;

public record WorldDependencies(
    TimeProvider Time,
    PersistenceModel Persistence,
    SessionModel Session,
    NetMessageCodec Codec,
    FrameProvider Frames);

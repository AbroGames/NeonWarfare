using System;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Composition;

public record WorldDependencies(TimeProvider Time, PersistenceModel Persistence, SessionModel Session);

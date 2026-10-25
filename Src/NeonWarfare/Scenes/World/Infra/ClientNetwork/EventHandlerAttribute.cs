using System;
using JetBrains.Annotations;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Infra.ClientNetwork;

/// <summary>
/// A Presentation method that <see cref="EventDispatcher"/> calls with every received event of its parameter type.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute;

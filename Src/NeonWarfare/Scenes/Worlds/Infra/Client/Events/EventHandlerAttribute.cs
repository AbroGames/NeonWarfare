using System;
using JetBrains.Annotations;

namespace NeonWarfare.Scenes.Worlds.Infra.Client.Events;

/// <summary>
/// Marks a Presentation method that <see cref="EventDispatcher"/> calls with every received event of its parameter
/// type.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute;

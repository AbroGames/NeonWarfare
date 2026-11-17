using System;

namespace NeonWarfare.Scenes.World.Infra.Entities;

/// <summary>
/// An entity the save keeps without its state: the save writes only its kind, NetId and parent, and a load creates
/// it fresh from its kind. So every replicated member must be a readonly field or set in the constructor, never null
/// after a load.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NotSavedAttribute : Attribute;

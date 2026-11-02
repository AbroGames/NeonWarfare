using System;

namespace NeonWarfare.Scenes.World.Entities;

/// <summary>
/// An entity the save keeps without its state: the save writes only its scene, NetId and parent, and a load gets
/// it fresh from the scene. So every replicated member must be a readonly field or set in the constructor, never null
/// after a load.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NotSavedAttribute : Attribute;

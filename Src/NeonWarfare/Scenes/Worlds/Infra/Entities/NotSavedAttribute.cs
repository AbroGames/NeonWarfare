using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// An entity the save keeps without its state, see Docs/Data-and-saves.md#models-and-storages.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NotSavedAttribute : Attribute;

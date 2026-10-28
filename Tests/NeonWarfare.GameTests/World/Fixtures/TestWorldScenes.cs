using System.Reflection;
using Godot;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Entities;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// A ready <see cref="WorldPackedScenes"/> without the game's <c>res://</c>: every scene is an empty node of its own.
/// </summary>
public static class TestWorldScenes
{
    public static WorldPackedScenes Create()
    {
        WorldPackedScenes scenes = CreateNotReady();
        scenes._Ready();
        return scenes;
    }

    /// <summary>The scenes are set, but the list is empty until <c>_Ready</c>, as in the game.</summary>
    public static WorldPackedScenes CreateNotReady()
    {
        var scenes = new WorldPackedScenes();
        IEnumerable<PropertyInfo> exports = typeof(WorldPackedScenes)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(PackedScene)
                               && property.IsDefined(typeof(ExportAttribute)));
        foreach (PropertyInfo export in exports)
        {
            scenes.Set(export.Name, Pack(new Node()));
        }
        return scenes;
    }

    /// <summary>The catalog of <paramref name="scenes"/> and the game's node types, as the game builds it.</summary>
    public static EntityCatalog CreateCatalog(WorldPackedScenes scenes) =>
        new(scenes.GetScenesList(), NetMessageCodecTests.CreateMapping().Types);

    public static PackedScene Pack(Node content)
    {
        var scene = new PackedScene();
        scene.Pack(content);
        content.Free();
        return scene;
    }
}

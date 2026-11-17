using System.Reflection;
using Godot;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// A ready <see cref="WorldPackedScenes"/> without the game's <c>res://</c>: every scene is an empty node of its own.
/// </summary>
public static class TestWorldScenes
{
    public static WorldPackedScenes Create()
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
        scenes._Ready();
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

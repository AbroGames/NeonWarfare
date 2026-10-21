using System.Reflection;
using Godot;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// A ready <see cref="WorldPackedScenes"/> without the game's <c>res://</c>: the storages are packed from their
/// classes, so the server spawn works, and every other scene is an empty node of its own.
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
            Node content = export.Name switch
            {
                nameof(WorldPackedScenes.PersistenceStorage) => new PersistenceStorage(),
                nameof(WorldPackedScenes.SessionStorage) => new SessionStorage(),
                _ => new Node(),
            };
            scenes.Set(export.Name, Pack(content));
        }
        scenes._Ready();
        return scenes;
    }

    public static PackedScene Pack(Node content)
    {
        var scene = new PackedScene();
        scene.Pack(content);
        content.Free();
        return scene;
    }
}

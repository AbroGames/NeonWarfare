using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// The kind id of every entity the World may spawn, see Docs/Networking.md#entities-and-replication.
/// <see cref="Descriptors"/> go into the protocol hash.
/// The catalog belongs to <c>GameProtocol</c>, not to the World, because a client needs the hash before its World
/// exists.
/// </summary>
public class EntityCatalog
{
    private readonly IReadOnlyList<PackedScene> _scenes;
    private readonly IReadOnlyList<Type> _types;
    // Keyed by reference, because Godot loads one instance per scene resource.
    private readonly Dictionary<PackedScene, int> _idByScene = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Type, int> _idByType = new();

    public IReadOnlyList<string> Descriptors { get; }

    public EntityCatalog(IReadOnlyList<PackedScene> scenes, IReadOnlyList<Type> mappedTypes)
    {
        _scenes = scenes;
        _types = mappedTypes.Where(IsCreatableNode).ToList();
        for (int i = 0; i < _scenes.Count; i++)
        {
            _idByScene.TryAdd(_scenes[i], i);
        }
        for (int i = 0; i < _types.Count; i++)
        {
            _idByType.Add(_types[i], _scenes.Count + i);
        }

        // The descriptor is the full resource path, so another scene with the same name changes the hash.
        Descriptors = _scenes.Select(scene => "scene " + scene.ResourcePath)
            .Concat(_types.Select(type => "type " + type.FullName))
            .ToList();
    }

    /// <exception cref="ArgumentException">The scene is not in the catalog.</exception>
    public int GetKindId(PackedScene scene)
    {
        if (scene != null && _idByScene.TryGetValue(scene, out int id)) return id;
        throw new ArgumentException($"{scene?.ResourcePath} is not a scene of the entity catalog.", nameof(scene));
    }

    /// <exception cref="ArgumentException">The type is not a node type of the catalog.</exception>
    public int GetKindId(Type type)
    {
        if (type != null && _idByType.TryGetValue(type, out int id)) return id;
        throw new ArgumentException($"{type?.FullName} is not a node type of the entity catalog.", nameof(type));
    }

    /// <summary>Creates a node of the kind outside the tree. A node of a type kind is named after its type.</summary>
    /// <exception cref="ArgumentException">The id is not in the catalog.</exception>
    public Node Create(int kindId)
    {
        if (kindId >= 0 && kindId < _scenes.Count) return _scenes[kindId].Instantiate();

        int typeIndex = kindId - _scenes.Count;
        if (typeIndex < 0 || typeIndex >= _types.Count)
        {
            throw new ArgumentException($"Kind id {kindId} is not in the entity catalog.", nameof(kindId));
        }

        Type type = _types[typeIndex];
        var node = (Node) Activator.CreateInstance(type)!;
        node.Name = type.Name;
        return node;
    }

    private static bool IsCreatableNode(Type type) =>
        type.IsSubclassOf(typeof(Node))
        && type is { IsAbstract: false, ContainsGenericParameters: false }
        && type.GetConstructor(Type.EmptyTypes) != null;
}

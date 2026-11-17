using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Humanizer;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// Every kind of entity World may spawn, by kind id: first the scenes in their catalog order, then every concrete
/// <see cref="Node"/> type of the type mapping in mapping order, created by its parameterless constructor. The id
/// travels in spawn records and lies in saves, so <see cref="Descriptors"/> go into the protocol hash. Owned by Game,
/// not World: the client needs the hash before its World exists.
/// </summary>
public class EntityCatalog
{
    private const string UnknownSceneError = "{0} is not a scene of the entity catalog.";
    private const string UnknownTypeError = "{0} is not a node type of the entity catalog.";
    private const string UnknownIdError = "Kind id {0} is not in the entity catalog.";

    private readonly IReadOnlyList<PackedScene> _scenes;
    private readonly IReadOnlyList<Type> _types;
    // By reference: Godot hands out one instance per loaded scene resource
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

        // The path, not a property name, so a different scene under the same name is seen too
        Descriptors = _scenes.Select(scene => "scene " + scene.ResourcePath)
            .Concat(_types.Select(type => "type " + type.FullName))
            .ToList();
    }

    /// <exception cref="ArgumentException">The scene is not in the catalog.</exception>
    public int GetKindId(PackedScene scene)
    {
        if (scene != null && _idByScene.TryGetValue(scene, out int id)) return id;
        throw new ArgumentException(UnknownSceneError.FormatWith(scene?.ResourcePath), nameof(scene));
    }

    /// <exception cref="ArgumentException">The type is not a node type of the catalog.</exception>
    public int GetKindId(Type type)
    {
        if (type != null && _idByType.TryGetValue(type, out int id)) return id;
        throw new ArgumentException(UnknownTypeError.FormatWith(type?.FullName), nameof(type));
    }

    /// <summary>A new node of the kind, not in the tree. A type kind is named after its type.</summary>
    /// <exception cref="ArgumentException">The id is not in the catalog.</exception>
    public Node Create(int kindId)
    {
        if (kindId >= 0 && kindId < _scenes.Count) return _scenes[kindId].Instantiate();

        int typeIndex = kindId - _scenes.Count;
        if (typeIndex < 0 || typeIndex >= _types.Count)
        {
            throw new ArgumentException(UnknownIdError.FormatWith(kindId), nameof(kindId));
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

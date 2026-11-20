using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Humanizer;

namespace NeonWarfare.Scenes.World.Infra.Entities;

/// <summary>
/// NetId and node of every spawned entity. A node leaves on <c>TreeExiting</c>, which Godot propagates to every
/// descendant, so freeing or removing an ancestor takes the whole registered subtree out.
/// </summary>
public class EntityRegistry : IEntityFinder
{
    private const string NoneError = "NetId.None cannot be registered ({0}).";
    private const string IdTakenError = "{0} is already registered to {1}.";
    private const string NodeTakenError = "{0} is already registered as {1}.";
    private const string NotFoundError = "{0} is not registered.";

    public const string NotSingleError =
        "Expected exactly one {0}, found {1}. An entity the world has from its start is missing only before the "
        + "World is initialized or the world snapshot is applied: do not read it in a service constructor.";

    private record Entry(NetId Id, Action OnTreeExiting);

    // Members are kept up to date from the first request on; the snapshot is rebuilt lazily after a change, so
    // a caller iterating it may spawn or despawn freely
    private class TypeCache(Type type)
    {
        public Type Type { get; } = type;
        public HashSet<NetId> Members { get; } = [];
        public Array Snapshot { get; set; }
    }

    private readonly Dictionary<NetId, Node> _nodeById = new();
    private readonly Dictionary<Node, Entry> _entryByNode = new();
    private readonly Dictionary<Type, TypeCache> _cacheByType = new();

    public event Action<NetId, Node> SpawnedEvent;

    public event Action<NetId, Node> DespawnedEvent;

    public void Register(NetId id, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (id == NetId.None) throw new ArgumentException(NoneError.FormatWith(node.Name), nameof(id));
        if (_nodeById.TryGetValue(id, out Node taken))
        {
            throw new InvalidOperationException(IdTakenError.FormatWith(id, taken.Name));
        }
        if (_entryByNode.TryGetValue(node, out Entry existing))
        {
            throw new InvalidOperationException(NodeTakenError.FormatWith(node.Name, existing.Id));
        }

        // A closure over the registry, not a node method: Godot would not disconnect it, so Remove does
        void OnTreeExiting() => Remove(node);
        _nodeById.Add(id, node);
        _entryByNode.Add(node, new Entry(id, OnTreeExiting));
        node.TreeExiting += OnTreeExiting;
        foreach (TypeCache cache in _cacheByType.Values.Where(cache => cache.Type.IsInstanceOfType(node)))
        {
            cache.Members.Add(id);
            cache.Snapshot = null;
        }

        SpawnedEvent?.Invoke(id, node);
    }

    public bool TryGetNode(NetId id, out Node node) => _nodeById.TryGetValue(id, out node);

    public Node GetNode(NetId id) =>
        _nodeById.TryGetValue(id, out Node node)
            ? node
            : throw new KeyNotFoundException(NotFoundError.FormatWith(id));

    public bool TryGetNetId(Node node, out NetId id)
    {
        bool found = _entryByNode.TryGetValue(node, out Entry entry);
        id = found ? entry.Id : NetId.None;
        return found;
    }

    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        if (!_cacheByType.TryGetValue(typeof(T), out TypeCache cache))
        {
            cache = new TypeCache(typeof(T));
            foreach ((NetId id, Node node) in _nodeById)
            {
                if (node is T) cache.Members.Add(id);
            }
            _cacheByType.Add(typeof(T), cache);
        }

        cache.Snapshot ??= cache.Members
            .OrderBy(id => id.Value)
            .Select(id => (T) (object) _nodeById[id])
            .ToArray();
        return (T[]) cache.Snapshot;
    }

    public T GetSingle<T>() where T : class
    {
        IReadOnlyList<T> found = GetAll<T>();
        return found.Count == 1
            ? found[0]
            : throw new InvalidOperationException(NotSingleError.FormatWith(typeof(T).Name, found.Count));
    }

    public bool Exists<T>() where T : class => GetAll<T>().Count > 0;

    private void Remove(Node node)
    {
        if (!_entryByNode.Remove(node, out Entry entry)) return;

        _nodeById.Remove(entry.Id);
        node.TreeExiting -= entry.OnTreeExiting;
        foreach (TypeCache cache in _cacheByType.Values)
        {
            if (cache.Members.Remove(entry.Id)) cache.Snapshot = null;
        }

        DespawnedEvent?.Invoke(entry.Id, node);
    }
}

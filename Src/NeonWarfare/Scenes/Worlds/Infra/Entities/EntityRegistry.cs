using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// The NetId, kind and node of every spawned entity.
/// A node leaves the registry on <c>TreeExiting</c>. Godot raises it on every descendant too, so freeing or removing
/// an ancestor takes the whole registered subtree out.
/// </summary>
public class EntityRegistry : IEntityFinder
{
    private record Entry(NetId Id, int KindId, Action OnTreeExiting);

    // The entities of one type, kept up to date from the first request on. A change only drops the snapshot, and the
    // next request builds a new one, so a caller iterating a snapshot may spawn and despawn freely.
    private class TypeCache(Type type)
    {
        public Type Type { get; } = type;
        public HashSet<NetId> Members { get; } = [];
        public Array Snapshot { get; set; }
    }

    private readonly Dictionary<NetId, Node> _nodeById = new();
    private readonly Dictionary<Node, Entry> _entryByNode = new();
    private readonly Dictionary<Type, TypeCache> _cacheByType = new();

    public event Action<NetId, Node> Spawned;

    public event Action<NetId, Node> Despawned;

    /// <param name="kindId">The <see cref="EntityCatalog"/> kind the node was created from.</param>
    public void Register(NetId id, Node node, int kindId)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (id == NetId.None)
        {
            throw new ArgumentException($"NetId.None cannot be registered ({node.Name}).", nameof(id));
        }
        if (_nodeById.TryGetValue(id, out Node taken))
        {
            throw new InvalidOperationException($"{id} is already registered to {taken.Name}.");
        }
        if (_entryByNode.TryGetValue(node, out Entry existing))
        {
            throw new InvalidOperationException($"{node.Name} is already registered as {existing.Id}.");
        }

        // Godot does not disconnect a closure when the node is freed, so Remove detaches it.
        void OnTreeExiting() => Remove(node);
        _nodeById.Add(id, node);
        _entryByNode.Add(node, new Entry(id, kindId, OnTreeExiting));
        node.TreeExiting += OnTreeExiting;
        foreach (TypeCache cache in _cacheByType.Values.Where(cache => cache.Type.IsInstanceOfType(node)))
        {
            cache.Members.Add(id);
            cache.Snapshot = null;
        }

        Spawned?.Invoke(id, node);
    }

    public bool TryGetNode(NetId id, out Node node) => _nodeById.TryGetValue(id, out node);

    public Node GetNode(NetId id) =>
        _nodeById.TryGetValue(id, out Node node)
            ? node
            : throw new KeyNotFoundException($"{id} is not registered.");

    public int GetKindId(NetId id) => _entryByNode[GetNode(id)].KindId;

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
            : throw new InvalidOperationException(
                $"Expected exactly one {typeof(T).Name}, found {found.Count}. An entity the world has from its "
                + "start is missing only before the World is initialized or the world snapshot is applied: do not "
                + "read it in a service constructor.");
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

        Despawned?.Invoke(entry.Id, node);
    }
}

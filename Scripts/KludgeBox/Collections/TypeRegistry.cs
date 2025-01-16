using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scripts.KludgeBox.Collections;

public abstract class TypeRegistry
{
    public bool IsSynchronized { get; set; } = true;
    public int RegisteredTypesCount => _nextId;
    
    private int _nextId = 0;
    private readonly Dictionary<int, Type> _idToType = new();
    private readonly Dictionary<Type, int> _typeToId = new();
    public Type TargetType { get; private set; }

    /// <summary>
    /// Creates new type registry.
    /// </summary>
    /// <param name="type">Base type for all types registered in this registry. If not presnt will default to Object.</param>
    public TypeRegistry(Type type = null)
    {
        TargetType = type ?? typeof(Object);
        ClearRegistry();
    }
    
    
    /// <summary>
    /// Registers a type and return its id. If type already registered, returns its id anyways.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public int RegisterType(Type type)
    {
        if (!type.IsAssignableTo(TargetType)) throw new ArgumentException($"Type '{type.FullName}' is not an instance of target type {TargetType.FullName}");
        if (_typeToId.TryGetValue(type, out var typeId)) return typeId;

        var id = _nextId;
        _idToType[id] = type;
        _typeToId[type] = id;
        _nextId++;
        Log.Debug($"Registered type '{type.FullName}' with id {id}");
        return id;
    }

    public Type GetType(int id)
    {
        if (!IsSynchronized && id != 0) throw new InvalidOperationException("Type registry must be synchronized with server first.");
        return _idToType[id];
    }

    public int GetTypeId(Type type)
    {
        return _typeToId[type];
    }

    /// <summary>
    /// Returns list of full type names ordered by id
    /// </summary>
    /// <returns></returns>
    public List<string> BuildSynchronizedSequence()
    {
        var types = new List<string>();

        for (int i = 0; i < _nextId; i++)
        {
            types.Add(GetType(i).FullName);
        }

        return types;
    }

    /// <summary>
    /// Reorders the contents of the registry according to the list
    /// </summary>
    /// <param name="types"></param>
    public void SynchronizeRegistry(List<string> types)
    {
        var registeredTypes = _idToType.Values.ToList();
        ClearRegistry();
        
        for (var i = 0; i < types.Count; i++)
        {
            var typeName = types[i];
            RegisterType(registeredTypes.Single(t => t.FullName == typeName));
        }

        IsSynchronized = true;
    }

    private void ClearRegistry()
    {
        _idToType.Clear();
        _typeToId.Clear();
        _nextId = 0;
    }
}

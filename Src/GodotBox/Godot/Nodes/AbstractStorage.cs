using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace GodotBox.Godot.Nodes;

public abstract partial class AbstractStorage : Node
{
    
    public IReadOnlyList<PackedScene> GetScenesList() => _scenesList.AsReadOnly();
    public IReadOnlyDictionary<string, PackedScene> GetScenesDictionary() => _sceneByName.AsReadOnly();
    
    private readonly Dictionary<string, PackedScene> _sceneByName = new();
    private readonly List<PackedScene> _scenesList = new();
    // By reference: Godot hands out one instance per loaded scene resource
    private readonly Dictionary<PackedScene, int> _idByScene = new(ReferenceEqualityComparer.Instance);
    
    /// <summary>
    /// Called in sealed <see cref="AbstractStorage._Ready()"/> before scanning for scenes.
    /// </summary>
    public virtual void _PreReady()
    {
        
    }
    
    public sealed override void _Ready()
    {
        _PreReady();
        RegisterScenes(this);
    }
        
    public bool TryGetScene(string name, out PackedScene scene)
    {
        return _sceneByName.TryGetValue(name, out scene);
    }

    /// <summary>The index of the scene in <see cref="GetScenesList"/>.</summary>
    public int GetSceneId(PackedScene scene)
    {
        if (scene != null && _idByScene.TryGetValue(scene, out int id)) return id;
        throw new ArgumentException($"{scene?.ResourcePath} is not a scene of {GetType().Name}", nameof(scene));
    }
    
    private void RegisterScenes(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        Type type = obj.GetType();
        foreach (PropertyInfo property in type.GetProperties(
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!property.PropertyType.IsAssignableTo(typeof(PackedScene))) continue;
            if (!Attribute.IsDefined(property, typeof(ExportAttribute))) continue;

            var scene = property.GetValue(this) as PackedScene;
            _sceneByName[property.Name] = scene;
            if (scene != null) _idByScene.TryAdd(scene, _scenesList.Count);
            _scenesList.Add(scene);
        }
    }
}
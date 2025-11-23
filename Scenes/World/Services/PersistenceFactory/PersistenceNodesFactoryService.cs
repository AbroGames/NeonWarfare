using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using NeonWarfare.Scenes.World.Services.StartStop;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.ParentInjection;
using Serilog;

namespace NeonWarfare.Scenes.World.Services.PersistenceFactory;

public partial class PersistenceNodesFactoryService : Node
{

    private Dictionary<Type, IPersistenceNodeFactory> _factories;
    
    [Parent] private World _world;
    [Logger] private ILogger _log;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        // Get all classes implementing IPersistenceNodeFactory
        _factories = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IPersistenceNodeFactory).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .Select(t => (IPersistenceNodeFactory) Activator.CreateInstance(t))
            .ToDictionary(t => t.CreatedType(), t => t);
        
        foreach (var factory in _factories.Values)
        {
            factory.InitFactory(_world);
        }
    }

    public TNode Create<TNode, TData>(Action<TData> init) where TNode : Node
    {
        _factories.TryGetValue(typeof(TNode), out var factory);
        if (factory == null)
        {
            _log.Error("Factory for type {type} not found.", typeof(TNode).Name);
            return null;
        }

        IPersistenceNodeFactory<TNode, TData> castedFactory = (IPersistenceNodeFactory<TNode, TData>) factory;
        return castedFactory.Create(init);
    }
}
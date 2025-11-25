using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.Service;


public class StatusEffectTypesStorageService
{
    //TODO В сервисы, где остальные сервисы GodotTemplate
    public static StatusEffectTypesStorageService Instance { get; } = new StatusEffectTypesStorageService();
    
    private readonly Dictionary<int, Type> _typeById = new();
    private readonly Dictionary<Type, int> _idByType = new();

    [Logger] private ILogger _log;
    
    public StatusEffectTypesStorageService()
    {
        Di.Process(this);
        
        // Find and sort all child of AbstractStatusEffect (except abstract)
        List<Type> types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AbstractClientStatusEffect)))
            .Where(t => !t.IsAbstract)
            .OrderBy(t => t.FullName)
            .ToList();

        for (int i = 0; i < types.Count; i++)
        {
            Type type = types[i];
            
            _typeById[i] = type;
            _idByType[type] = i;

            _log.Debug("Add effect: {id} -> {name}", i, type.Name);
        }

        _log.Information("Initialized with {count} effects.", _typeById.Count);
    }

    public int GetId(Type type)
    {
        if (_idByType.TryGetValue(type, out int id))
        {
            return id;
        }
        throw new KeyNotFoundException($"Type {type.Name} is not found in {nameof(StatusEffectTypesStorageService)}");
    }
    
    public int GetId<T>() where T : AbstractStatusEffect
    {
        return GetId(typeof(T));
    }

    public Type GetType(int id)
    {
        if (_typeById.TryGetValue(id, out Type type))
        {
            return type;
        }
        throw new KeyNotFoundException($"Id {id} is not found in {nameof(StatusEffectTypesStorageService)}.");
    }
}
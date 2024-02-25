using System;
using System.Collections.Generic;
using KludgeBox;

namespace AbroDraft.Scripts.Utils;

public class ServiceRegistry
{
    public List<Object> Services { get; private set; } = new();

    public void Register(object service)
    {
        Services.Add(service);
    }

    public void RegisterServices()
    {
        var types = ReflectionExtensions.FindTypesWithAttributes(typeof(GameServiceAttribute));
        foreach (Type type in types)
        {
            try
            {
                var instance = type.GetInstanceOfType();
                Register(instance);
            }
            catch (Exception e)
            {
                Log.Error($"Can't instantiate service {type.FullName}:\n{e.Message}");
            }
            
        }
    }
}

public class GameServiceAttribute : Attribute;
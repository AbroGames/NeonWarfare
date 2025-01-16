using System;
using System.Collections.Generic;

namespace NeonWarfare.Scripts.Utils.InstanceRouting;

/// <summary>
/// Вспомогательные статические утилиты для маппинга типа сущности в тип её резолвера через чтение интерфейса <see cref="IResolvableBy{TResolver}"/>
/// </summary>
public static class RoutingUtils
{
    private static Dictionary<Type, Type> _resolversCache = new();
    
    /// <summary>
    /// Извлекает и кэширует тип резолвера для указанного типа сущности из интерфейса <see cref="IResolvableBy{TResolver}"/>
    /// </summary>
    /// <param name="instanceType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Type GetInstanceResolverTypeForType(this Type instanceType)
    {
        if (instanceType.TryGetInstanceResolverTypeForType(out var instanceResolverType))
        {
            return instanceResolverType;
        }
        
        throw new ArgumentException($"Type '{instanceType.FullName}' is not resolvable.");
    }
    
    public static bool TryGetInstanceResolverTypeForType(this Type instanceType, out Type instanceResolverType)
    {
        instanceResolverType = null;
        if (!instanceType.IsAssignableTo(typeof(IResolvable)))
        {
            return false;
        }

        if (_resolversCache.TryGetValue(instanceType, out instanceResolverType))
        {
            return true;
        }

        // Get all interfaces implemented by instanceType
        var interfaces = instanceType.GetInterfaces();

        // Check if any interface matches IResolvableBy<TResolver> where TResolver : IInstanceResolver
        foreach (var iface in interfaces)
        {
            // Check if the interface is a generic type and matches the generic type definition IResolvableBy<>
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IResolvableBy<>))
            {
                var genericArgument = iface.GetGenericArguments()[0];

                // Check if the generic argument (TResolver) is assignable to IInstanceResolver
                if (genericArgument.IsAssignableTo(typeof(IInstanceResolver)))
                {
                    instanceResolverType = genericArgument;
                    _resolversCache.Add(instanceType, instanceResolverType);
                    return true;
                }
            }
        }
        
        return false;
    }
}

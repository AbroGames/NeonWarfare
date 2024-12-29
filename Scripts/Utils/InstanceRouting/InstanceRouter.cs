using System;
using System.Collections.Generic;
using NeonWarfare.Utils.InstanceRouting.Policies;

namespace NeonWarfare.Utils.InstanceRouting;

/// <summary>
/// Главный класс, отвечающий за резолвинг сущностей. В теории достаточно создать один экземпляр этого класса и он будет работать без дополнительных ручных настроек.
/// </summary>
public sealed class InstanceRouter
{
    // map resolver types to theirs providing policies.
    private readonly Dictionary<Type, IInstanceResolverProvidingPolicy> _resolverPolicies = new();
    private readonly Dictionary<Type, IInstanceResolver> _overridenResolvers = new();

    /// <summary>
    /// Находит сущность указанного типа по некоторому ключу.
    /// </summary>
    /// <param name="instanceType">Тип сущности, которую надо найти</param>
    /// <param name="key">Ключ для поиска. В зависимости от алгоритма резолвера может быть чем угодна, от целочисленного Nid, до целого пакета</param>
    /// <returns></returns>
    public object Resolve(Type instanceType, object key)
    {
        var resolver = GetInstanceResolver(instanceType);
        var result = resolver.ResolveInstance(key);
        
        return result;
    }

    /// <summary>
    /// Принудительно устанавливает резолвер для указанного типа.
    /// </summary>
    /// <remarks>
    /// Указанный здесь резолвер будет иметь более высокий приоритет, чем тот, что указан в интерфейсе <see cref="IResolvableBy{TResolver}"/>
    /// </remarks>
    /// <param name="instanceType">Тип сущности, которую надо найти</param>
    /// <param name="resolver">Резолвер для поиска сущности указанного типа</param>
    public void UseInstanceResolverForType(Type instanceType, IInstanceResolver resolver)
    {
        _overridenResolvers[instanceType] = resolver;
    }

    /// <summary>
    /// Удаляет принудительное переопределение резолвера для указанного типа. Если тип не имеет интерфейса <see cref="IResolvableBy{TResolver}"/>, он больше не сможет быть зарезолвен.
    /// </summary>
    /// <param name="instanceType"></param>
    public void RemoveResolverOverrideForType(Type instanceType)
    {
        _overridenResolvers.Remove(instanceType);
    }

    private IInstanceResolver GetInstanceResolver(Type instanceType)
    {
        if (_overridenResolvers.TryGetValue(instanceType, out var instanceResolverOverride))
        {
            if(instanceResolverOverride is not null)
                return instanceResolverOverride;
        }
        
        var resloverType = instanceType.GetInstanceResolverTypeForType();
        var policy = GetResolverPolicyForResolverType(resloverType);

        return policy.GetInstanceResolver();
    }
    
    private IInstanceResolverProvidingPolicy GetResolverPolicyForResolverType(Type instanceResolverType)
    {
        IInstanceResolverProvidingPolicy policy = null;
        if (!_resolverPolicies.TryGetValue(instanceResolverType, out policy))
        {
            policy = UseDefaultResolverProvidingPolicyForResolverType(instanceResolverType);
        }
        
        return policy;
    }

    private IInstanceResolverProvidingPolicy UseDefaultResolverProvidingPolicyForResolverType(Type instanceResolverType)
    {
        var policy = new CreateAndCache_InstanceResolverProvidingPolicy(instanceResolverType);
        _resolverPolicies.Add(instanceResolverType, policy);
        return policy;
    }
}
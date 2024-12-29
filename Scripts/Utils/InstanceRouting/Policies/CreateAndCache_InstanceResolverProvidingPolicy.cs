using System;
using KludgeBox;

namespace NeonWarfare.Utils.InstanceRouting.Policies;

/// <summary>
/// Дефолтная политика получения резолвера - создать и сохранить для переиспользования.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CreateAndCache_InstanceResolverProvidingPolicy : IInstanceResolverProvidingPolicy
{
    protected IInstanceResolver _instanceResolver;
    
    public virtual Type ResolverType { get; }
    public IInstanceResolver GetInstanceResolver()
    {
        if (_instanceResolver is null)
        {
            CreateAndCacheInstanceResolver();
        }
        
        return _instanceResolver;
    }

    public CreateAndCache_InstanceResolverProvidingPolicy(Type resolverType)
    {
        if (!resolverType.IsAssignableTo(typeof(IInstanceResolver)))
        {
            throw new ArgumentException($"Type {resolverType} is not an IInstanceResolver");
        }

        if (!resolverType.HasParameterlessConstructor())
        {
            throw new ArgumentException($"Type {resolverType} must have a parameterless constructor for CreateAndCacheInstanceResolverProvidingPolicy");
        }
        
        ResolverType = resolverType;
    }

    protected virtual void CreateAndCacheInstanceResolver()
    {
        _instanceResolver = Activator.CreateInstance(ResolverType) as IInstanceResolver;
    }
}

public class CreateAndCache_InstanceResolverProvidingPolicy<TResolver>()
    : CreateAndCache_InstanceResolverProvidingPolicy(typeof(TResolver))
    where TResolver : IInstanceResolver, new()
{
    protected override void CreateAndCacheInstanceResolver()
    {
        _instanceResolver = new TResolver();
    }
}
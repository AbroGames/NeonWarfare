using System;

namespace NeonWarfare.Scripts.Utils.InstanceRouting.Policies;

/// <summary>
/// Дополнительная неиспользуемая политика получения резолвера - вернуть заранее созданный экземпляр резолвера
/// TODO: Подумать где оно может пригодиться и можно ли удалить эту политику
/// </summary>
public class UseExisting_InstanceResolverProvidingPolicy : IInstanceResolverProvidingPolicy
{
    public IInstanceResolver InstanceResolver { get; }
    public Type ResolverType { get; }

    public UseExisting_InstanceResolverProvidingPolicy(IInstanceResolver instanceResolver) : this(instanceResolver, instanceResolver.GetType()) {}

    public UseExisting_InstanceResolverProvidingPolicy(IInstanceResolver instanceResolver, Type resolverType)
    {
        InstanceResolver = instanceResolver;
        ResolverType = resolverType;
    }
    
    public IInstanceResolver GetInstanceResolver()
    {
        return InstanceResolver;
    }
}

using System;

namespace NeonWarfare.Scripts.Utils.InstanceRouting.Policies;

/// <summary>
/// Интерфейс описывает логику получения резолвера указанного типа.
/// </summary>
public interface IInstanceResolverProvidingPolicy
{
    // TODO: проверить нужно ли вообще это свойство
    Type ResolverType { get; }
    IInstanceResolver GetInstanceResolver();
}

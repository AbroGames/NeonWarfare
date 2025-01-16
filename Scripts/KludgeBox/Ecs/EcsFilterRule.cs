using System;

namespace NeonWarfare.Scripts.KludgeBox.Ecs;

public sealed class EcsFilterRule(Type filterType, bool include)
{
    public Type FilterType { get; init; } = filterType;
    public bool Include { get; init; } = include;

    public bool Match(EcsEntity entity)
    {
        return Include ? entity.HasComponent(FilterType) : !entity.HasComponent(FilterType);
    }
}

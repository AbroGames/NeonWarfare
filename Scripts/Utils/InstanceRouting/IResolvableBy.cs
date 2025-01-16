using System;

namespace NeonWarfare.Scripts.Utils.InstanceRouting;

/// <summary>
/// Интерфейс - заглушка для строгой типизации в методах. Никогда не должен реализовываться напрямую. Вместо него используй <see cref="IResolvableBy{TResolver}"/>
/// </summary>
public interface IResolvable;

/// <summary>
/// Маркерный интерфейс, который несет в себе информацию об используемом резолвере для сущностей этого типа.
/// </summary>
/// <typeparam name="TResolver">Тип резолвера для сущностей этого типа</typeparam>
public interface IResolvableBy<TResolver> : IResolvable where TResolver : IInstanceResolver;

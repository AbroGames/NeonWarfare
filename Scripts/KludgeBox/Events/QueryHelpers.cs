using System;

namespace KludgeBox.Events;

public static class QueryHelpers
{
    public static Type GetGenericType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(QueryEvent<>))
        {
            return type.GetGenericArguments()[0];
        }

        Type baseType = type.BaseType;
        if (baseType != null)
        {
            return GetGenericType(baseType);
        }

        throw new ArgumentException("Тип не является поддерживаемым подтипом QueryEvent<T>.", nameof(type));
    }
}
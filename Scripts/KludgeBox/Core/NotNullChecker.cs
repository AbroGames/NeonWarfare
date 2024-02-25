using System;
using System.Reflection;

namespace KludgeBox;

public static class NotNullChecker
{
    public static void CheckProperties(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        Type type = obj.GetType();
        foreach (PropertyInfo property in type.GetProperties())
        {
            bool isNull = property.GetValue(obj) == null;
            bool hasNotNullAttribute = Attribute.IsDefined(property, typeof(NotNullAttribute));
            if (hasNotNullAttribute && isNull)
            {
                Log.Critical($"Property '{property.Name}' is null, but has NotNull attribute.");
            }
        }
    }
}
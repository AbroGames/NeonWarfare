using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot.Collections;

namespace KludgeBox;

public static class NotNullChecker
{

    private static readonly HashSet<Type> Checked = new();
    
    public static void CheckProperties(object obj)
    {
        if (Checked.Contains(obj.GetType())) return;
        CheckPropertiesForce(obj);
    }
    
    public static void CheckPropertiesForce(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        Type type = obj.GetType();
        Log.Info("Type: " + type);
        foreach (PropertyInfo property in type.GetProperties())
        {
            bool isNull = property.GetValue(obj) == null;
            bool hasNotNullAttribute = Attribute.IsDefined(property, typeof(NotNullAttribute));
            if (hasNotNullAttribute && isNull)
            {
                Log.Critical($"Property '{property.Name}' in type '{obj.GetType()}' is null, but has NotNull attribute");
            }
        }

        Checked.Add(type);
    }
}
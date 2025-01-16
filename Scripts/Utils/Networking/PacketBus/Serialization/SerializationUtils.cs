using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

public static class SerializationUtils
{
    private static readonly Type[] IgnorableAttributes = [
        typeof(DoNotSerializeAttribute),
        typeof(NonSerializedAttribute),
        typeof(IgnoreDataMemberAttribute),
        typeof(JsonIgnoreAttribute),
        typeof(Newtonsoft.Json.JsonIgnoreAttribute)
    ];
    
    private static readonly Dictionary<Type, IMemberAccessor[]> CachedAccessors = new();
    
    public static IMemberAccessor[] GetSerializableMembers(this Type type)
    {
        if (CachedAccessors.TryGetValue(type, out var cachedAccessors))
        {
            return cachedAccessors;
        }
        
        var members = type.GetMembers();
        var accessors = new List<IMemberAccessor>();
        foreach (var member in members)
        {
            if (!ShouldBeSerialized(member))
                continue;

            if (member is PropertyInfo property)
            {
                accessors.Add(new PropertyAccessor(property));
            }

            if (member is FieldInfo field)
            {
                accessors.Add(new FieldAccessor(field));
            }
        }
        
        var orderedAccessors = accessors
            .OrderBy(accessor => accessor.Member.Name)
            .ToArray();
        
        CachedAccessors[type] = orderedAccessors;
        
        return orderedAccessors;
    }
    
    private static bool ShouldBeSerialized(MemberInfo member)
    {
        if (member is not (FieldInfo or PropertyInfo))
            return false;

        foreach (var attribute in member.GetCustomAttributes())
        {
            if (IgnorableAttributes.Contains(attribute.GetType()))
                return false;
        }

        if (member is PropertyInfo property)
        {
            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();
            
            if (getMethod is not null && !getMethod.IsStatic && getMethod.IsPublic)
                return false;
        
            if (setMethod is not null && !setMethod.IsStatic && setMethod.IsPublic)
                return false;
            
            return true;
        }

        if (member is FieldInfo field)
        {
            if (!field.IsPublic)
                return false;
            
            if (field.IsStatic)
                return false;
            
            return true;
        }
        
        return false;
    }
}

public interface IMemberAccessor
{
    MemberInfo Member { get; }
    Type ValueType { get; }
    public void SetValue(object target, object value);
    public object GetValue(object target);
}

public class FieldAccessor : IMemberAccessor
{
    public MemberInfo Member => _field;
    public Type ValueType => _field.FieldType;
    private FieldInfo _field;
    
    public FieldAccessor(FieldInfo field)
    {
        if(field.IsPrivate)
            throw new ArgumentException($"Field {field.Name} is not public");
        
        _field = field;
    }
    
    public void SetValue(object target, object value)
    {
        _field.SetValue(target, value);
    }

    public object GetValue(object target)
    {
        return _field.GetValue(target);
    }
}

public class PropertyAccessor : IMemberAccessor
{
    public MemberInfo Member => _property;
    public Type ValueType => _property.PropertyType;
    private PropertyInfo _property;

    public PropertyAccessor(PropertyInfo property)
    {
        if (property.GetGetMethod() is not null && property.GetGetMethod()!.IsPublic)
            throw new ArgumentException($"Property {property.Name} does not have a public getter.");
        
        if (property.GetSetMethod() is not null && property.GetSetMethod()!.IsPublic)
            throw new ArgumentException($"Property {property.Name} does not have a public setter.");
        
        _property = property;
    }
    
    public void SetValue(object target, object value)
    {
        _property.SetValue(target, value);
    }

    public object GetValue(object target)
    {
        return _property.GetValue(target);
    }
}

using System;
using System.Reflection;

namespace NeonWarfare.Scripts.KludgeBox.Core;

public static class MemberExtensions
{
    public static T GetAttribute<T>(this MemberInfo member) where T : Attribute
    {
        return member.GetCustomAttribute<T>();
    }
    
    public static bool TryGetAttribute<T>(this MemberInfo member, out T attribute) where T : Attribute
    {
        var attr = member.GetCustomAttribute<T>();
        attribute = attr;
        return attr != null;
    }
    
    public static Attribute GetAttribute(this MemberInfo member, Type attributeType)
    {
        return member.GetCustomAttribute(attributeType);
    }
    
    public static bool TryGetAttribute(this MemberInfo member, Type attributeType, out Attribute attribute)
    {
        var attr = member.GetCustomAttribute(attributeType);
        attribute = attr;
        return attr != null;
    }
    
    public static bool HasAttribute<T>(this MemberInfo member) where T : Attribute
    {
        return member.GetCustomAttribute<T>() != null;
    }
    
    public static bool HasAttribute(this MemberInfo member, Type attributeType)
    {
        return member.GetCustomAttribute(attributeType) != null;
    }
}
using System;

namespace KludgeBox;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotNullAttribute : Attribute
{
}
using System;

namespace NeonWarfare.Scenes.Screen.Menu.SettingsSystem;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class NameAttribute : Attribute
{
    public string Name { get; }

    public NameAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class HideAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class HintAttribute : Attribute
{
    public string Hint { get; }

    public HintAttribute(string hint)
    {
        Hint = hint;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class StepAttribute : Attribute
{
    public double Step { get; }
    public StepAttribute(double step)
    {
        Step = step;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RangeAttribute : Attribute
{
    public double Min { get; }
    public double Max { get; }
    public RangeAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CategoryAttribute : Attribute
{
    public string Category { get; }
    public CategoryAttribute(string category)
    {
        Category = category;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class OptionsAttribute : Attribute
{
    public string[] Options { get; }
    public OptionsAttribute(params string[] options)
    {
        Options = options;
    }
}

/// <summary>
/// The values of the <see cref="OptionsAttribute"/> items, in their order, of the member's type. Without it an item's
/// value is its label.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class OptionValuesAttribute : Attribute
{
    public object[] Values { get; }
    public OptionValuesAttribute(params object[] values)
    {
        Values = values;
    }
}

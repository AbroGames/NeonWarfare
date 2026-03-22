using System;

namespace Kludgeful.Main.SettingsSystem;

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
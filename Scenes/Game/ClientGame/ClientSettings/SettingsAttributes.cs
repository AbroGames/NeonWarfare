using System;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingForceColorAlphaAttribute(float alpha) : Attribute
{
    public float ForcedAlpha { get; } = alpha;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingForceColorValueAttribute(float value) : Attribute
{
    public float ForcedValue { get; } = value;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingNumberInputTypeAttribute(NumberInputType numberInputType) : Attribute
{
    public NumberInputType NumberInputType { get; } = numberInputType;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingNumberRangeAttribute(double min, double max) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;

    public SettingNumberRangeAttribute(double max) : this(0, max) {}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingNumberStepAttribute(double step) : Attribute
{
    public double Step { get; } = step;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingOptionsAttribute(params string[] options) : Attribute
{
    public string[] Options { get; } = options;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public abstract class SettingsOptionsProvidedByAttribute : Attribute
{
    public abstract IOptionsProvider GetOptionsProvider();
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SettingOptionsProvidedByAttribute<TOptionsProvider> : SettingsOptionsProvidedByAttribute
    where TOptionsProvider : IOptionsProvider, new()
{
    public override IOptionsProvider GetOptionsProvider() => new TOptionsProvider();
}
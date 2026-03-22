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
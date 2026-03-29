using System;
using System.Reflection;
using Humanizer;
using KludgeBox.Reflection.Access;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.SettingsSystem;

public record Setting
{
    public IMemberAccessor Member { get; init; }
    public string Name { get; init; }
    public string Hint { get; init; }
    public Type Type { get; init; }
    public object Target { get; init; }
    public object Value { get; set; }

    public Setting(IMemberAccessor source, object target)
    {
        Member = source;
        Name = source.Member.Name.Humanize().Titleize();
        Hint = source.Member.GetCustomAttribute<HintAttribute>()?.Hint;
        Value = source.GetValue(target);
        Type = source.ValueType;
        Target = target;
    }

    public void Apply()
    {
        Member.SetValue(Target, Value);
    }

    public void Deconstruct(out string name, out Type type, out object value)
    {
        name = Name;
        type = Type;
        value = Value;
    }
}
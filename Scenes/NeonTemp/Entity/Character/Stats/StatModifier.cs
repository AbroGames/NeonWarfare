using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

[MessagePackObject(AllowPrivate = true)]
public partial class StatModifier : ObservableObject //TODO Если меняется _value, то как это засинкать по сети? Или синкать только результирующие значения кеша? Только результаты кеша, мб прям в Client через Godot.Dictionary
{
    public enum ModifierType { Additive, Multiplicative }
        
    [Key(0)] public readonly Stat Stat;
    [Key(1)] public readonly ModifierType Type;
    [Key(2)] [ObservableProperty] private double _value;

    public StatModifier(Stat stat, ModifierType type, double value)
    {
        Stat = stat;
        Type = type;
        _value = value;
    }

    public static StatModifier CreateAdditive(Stat stat, double value)
    {
        return new StatModifier(stat, ModifierType.Additive, value);
    }
        
    public static StatModifier CreateMultiplicative(Stat stat, double value)
    {
        return new StatModifier(stat, ModifierType.Multiplicative, value);
    }
}
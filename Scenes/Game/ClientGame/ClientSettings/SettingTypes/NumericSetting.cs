using System;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public class NumericSetting : Setting
{
    public NumberInputType NumberInputType { get; } = NumberInputType.TextField;
    public double MinValue { get; } = 0;
    public double MaxValue { get; } = double.MaxValue;
    public double Step { get; } = 0;

    public NumericSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
        if (accessor.Member.TryGetAttribute<SettingNumberInputTypeAttribute>(out var numberInputTypeAttribute))
        {
            NumberInputType = numberInputTypeAttribute.NumberInputType;
        }

        if (accessor.Member.TryGetAttribute<SettingNumberRangeAttribute>(out var numberRangeAttribute))
        {
            MinValue = numberRangeAttribute.Min;
            MaxValue = numberRangeAttribute.Max;
        }

        if (accessor.Member.TryGetAttribute<SettingNumberStepAttribute>(out var numberStepAttribute))
        {
            Step = numberStepAttribute.Step;
        }
    }
    
    public override void SetValue(object value)
    {
        var trueValueType = Accessor.ValueType;
        object result = null;
        
        double doubleValue = Convert.ToDouble(value);
        doubleValue = Mathf.Snapped(doubleValue, Step);
        doubleValue = Math.Clamp(doubleValue, MinValue, MaxValue);
        
        if (trueValueType == typeof(int))
        {
            result = ProcessAsInt32(doubleValue);
        }

        if (trueValueType == typeof(long))
        {
            result = ProcessAsInt64(doubleValue);
        }

        if (trueValueType == typeof(double))
        {
            result = doubleValue;
        }

        if (trueValueType == typeof(float))
        {
            result = ProcessAsSingle(doubleValue);
        }
        
        base.SetValue(result);
    }

    private int ProcessAsInt32(double value)
    {
        value = Math.Clamp(value, Int32.MinValue, Int32.MaxValue);
        var intValue = Convert.ToInt32(value);
        
        return intValue;
    }

    private long ProcessAsInt64(double value)
    {
        value = Math.Clamp(value, Int64.MinValue, Int64.MaxValue);
        var longValue = Convert.ToInt64(value);
        
        return longValue;
    }

    private float ProcessAsSingle(double value)
    {
        value = Math.Clamp(value, Single.MinValue, Single.MaxValue);
        var floatValue = Convert.ToSingle(value);
        
        return floatValue;
    }
}
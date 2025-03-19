using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public abstract class Setting
{
    public string Name { get; }
    public string Description { get; }
    public IMemberAccessor Accessor { get; }
    public SettingType SettingType { get; }
    
    
    protected readonly Settings _settings;
    

    public Setting(IMemberAccessor accessor, Settings settings)
    {
        _settings = settings;
        Accessor = accessor;
        var valueType = accessor.ValueType;
        
        // First detect setting value type
        if(valueType == typeof(bool))
            SettingType = SettingType.Boolean;
        
        if(valueType == typeof(int)
           || valueType == typeof(long)
           || valueType == typeof(double)
           || valueType == typeof(float))
            SettingType = SettingType.Number;
        
        if(valueType == typeof(string))
            SettingType = SettingType.String;
        
        if(valueType == typeof(Color))
            SettingType = SettingType.Color;

        // Get name
        Name = accessor.Member.TryGetAttribute<SettingNameAttribute>(out var nameAttribute) ? nameAttribute.Name : accessor.Member.Name;

        // Get description
        Description = accessor.Member.TryGetAttribute<SettingDescriptionAttribute>(out var descriptionAttribute) ? descriptionAttribute.Description : null;

        
    }

    public virtual object GetValue()
    {
        return Accessor.GetValue(_settings);
    }

    public virtual void SetValue(object value)
    {
        Accessor.SetValue(_settings, value);
    }
}
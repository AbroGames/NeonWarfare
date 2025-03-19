using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingFactories;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings;

public static class SettingsUtils
{
    private static readonly Dictionary<SettingType, ISettingFactory> _settingFactories = new()
    {
        { SettingType.Boolean, new BooleanSettingFactory() },
        { SettingType.Color, new ColorSettingFactory() },
        { SettingType.Number, new NumericSettingFactory() },
        { SettingType.String, new StringSettingFactory() },
        { SettingType.Group, new SettingGroupSettingFactory() }
    };

    
    public static IReadOnlyList<Setting> GetSettings(this Settings settings)
    {
        var accessors = settings.GetType().GetSerializableMembers();
        var settingsList = new List<Setting>(accessors.Length);
        foreach (var memberAccessor in accessors)
        {
            try
            {
                settingsList.Add(
                    GetFactoryForSettingType(
                            GetSettingTypeForAccessor(memberAccessor))
                        .BuildSetting(memberAccessor, settings));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to get setting for {memberAccessor.Member.Name}: {e}");
            }
        }
        
        return settingsList;
    }
    
    private static ISettingFactory GetFactoryForSettingType(SettingType settingType)
    {
        return _settingFactories[settingType];
    }

    private static SettingType GetSettingTypeForAccessor(IMemberAccessor accessor)
    {
        var valueType = accessor.ValueType;
        SettingType settingType = SettingType.Unknown;
        
        // First detect setting value type
        if (valueType == typeof(bool))
            settingType = SettingType.Boolean;
        
        if (valueType == typeof(int)
           || valueType == typeof(long)
           || valueType == typeof(double)
           || valueType == typeof(float))
            settingType = SettingType.Number;
        
        if (valueType == typeof(string))
            settingType = SettingType.String;
        
        if (valueType == typeof(Color))
            settingType = SettingType.Color;
        
        if (valueType == typeof(SettingGroup))
            settingType = SettingType.Group;

        return settingType;
    }
}
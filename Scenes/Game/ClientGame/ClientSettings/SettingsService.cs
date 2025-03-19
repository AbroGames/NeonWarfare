using System;
using System.IO;
using System.Reflection;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FileAccess = Godot.FileAccess;

namespace NeonWarfare.Scripts.Utils.GameSettings;

public static class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new IgnoreTypeContractResolver(typeof(SettingGroup)),
        Formatting = Formatting.Indented
    };
    
    public static void Save(Settings settings)
    {
        var json = JsonConvert.SerializeObject(settings, _serializerSettings);
        using var file = FileAccess.Open(GetSettingsFilePath(), FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public static Settings Load()
    {
        try
        {
            using var file = FileAccess.Open(GetSettingsFilePath(), FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var state = JsonConvert.DeserializeObject<Settings>(json, _serializerSettings);
            state ??= new Settings();
            return state;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load {SettingsFileName}: {ex.Message}");
            var newSettings = new Settings();
            
            return new Settings();
        }
    }

    private static string GetSettingsFilePath()
    {
        return $"user://{SettingsFileName}";
    }
}

public class IgnoreTypeContractResolver : DefaultContractResolver
{
    private readonly Type _typeToIgnore;

    public IgnoreTypeContractResolver(Type typeToIgnore)
    {
        _typeToIgnore = typeToIgnore;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // Если тип свойства совпадает с указанным типом, игнорируем его
        if (property.PropertyType == _typeToIgnore)
        {
            property.ShouldSerialize = instance => false;
        }

        return property;
    }
}
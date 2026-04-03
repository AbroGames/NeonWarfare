using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KludgeBox.Reflection.Access;

namespace NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

public partial class MenuGameSettings
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerOptions.Default) { WriteIndented = true };

    static MenuGameSettings()
    {
        JsonSerializerOptions.Converters.Add(new ColorJsonConverter());
    }
    
    
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, GetType(), JsonSerializerOptions);
    }

    public static MenuGameSettings Deserialize(string json)
    {
        return Deserialize<MenuGameSettings>(json);
    }
    
    public static TType Deserialize<TType>(string json) where TType : MenuGameSettings
    {
        return JsonSerializer.Deserialize<TType>(json, JsonSerializerOptions);
    }
    
    public IReadOnlyList<Setting> GetVisibleSettings()
    {
        var accessors = GameSettingsInternals.VisibleAccessors;
        
        return accessors
            .Select(accessor => new Setting(accessor, this))
            .ToList();
    }

    public void SetVisibleSettings(IReadOnlyList<Setting> settings)
    {
        foreach (var setting in settings)
        {
            setting.Apply();
        }
    }
}

file static class GameSettingsInternals
{
    public static IList<IMemberAccessor> VisibleAccessors
    {
        get
        {
            if (_visibleAccessors is null)
            {
                _visibleAccessors = Services.MembersScanner.ScanMembers(typeof(MenuGameSettings))
                    .Where(accessor => accessor.IsPublic)
                    .Where(accessor => !accessor.HasAttribute<HideAttribute>())
                    .ToList();
            }
            return _visibleAccessors;
        }
    }
    private static IList<IMemberAccessor> _visibleAccessors;
}
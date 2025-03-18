using System.Collections.Generic;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public class StringSetting : Setting
{
    public IReadOnlyList<string> Options { get; set; }
    public bool HasOptions => Options.Count > 0;
    
    public StringSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
        Options = new List<string>();
        
        // Set options list if available
        if (accessor.Member.TryGetAttribute<SettingsOptionsProvidedByAttribute>(out var optionsProviderAttribute) && SettingType != SettingType.Boolean)
        {
            var options = new List<string>(optionsProviderAttribute.GetOptionsProvider().GetOptions());
            Options = options;
        }
        
        if (accessor.Member.TryGetAttribute<SettingOptionsAttribute>(out var optionsAttribute) && SettingType != SettingType.Boolean)
        {
            var options = new List<string>(optionsAttribute.Options);
            Options = options;
        }
    }
}
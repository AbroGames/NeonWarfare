using NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingFactories;

public class ColorSettingFactory : ISettingFactory
{
    public Setting BuildSetting(IMemberAccessor accessor, Settings settings)
    {
        return new ColorSetting(accessor, settings);
    }
}
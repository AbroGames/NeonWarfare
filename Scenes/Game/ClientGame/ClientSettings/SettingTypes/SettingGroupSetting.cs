using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public class SettingGroupSetting : Setting
{
    public SettingGroupSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
    }
}
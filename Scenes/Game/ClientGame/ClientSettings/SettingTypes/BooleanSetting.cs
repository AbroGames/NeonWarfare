using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public class BooleanSetting : Setting
{
    public BooleanSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
    }
}
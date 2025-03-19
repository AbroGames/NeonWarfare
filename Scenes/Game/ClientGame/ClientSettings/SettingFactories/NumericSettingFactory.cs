﻿using NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingFactories;

public class NumericSettingFactory : ISettingFactory
{
    public Setting BuildSetting(IMemberAccessor accessor, Settings settings)
    {
        return new NumericSetting(accessor, settings);
    }
}
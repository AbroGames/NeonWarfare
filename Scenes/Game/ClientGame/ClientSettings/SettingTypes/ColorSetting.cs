using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public class ColorSetting : Setting
{
    public float ForcedAlpha { get; }
    public bool HasForcedAlpha { get; }
    public ColorSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
        if (accessor.Member.TryGetAttribute<SettingForceColorAlphaAttribute>(out var forcedAlphaAttribute))
        {
            ForcedAlpha = forcedAlphaAttribute.ForcedAlpha;
            HasForcedAlpha = true;
        }
    }

    public override void SetValue(object value)
    {
        var colorValue = (Color)value;

        if (HasForcedAlpha)
        {
            colorValue.A = ForcedAlpha;
        }
        
        base.SetValue(colorValue);
    }
}
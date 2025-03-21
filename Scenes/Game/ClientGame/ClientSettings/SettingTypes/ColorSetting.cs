using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings.SettingTypes;

public class ColorSetting : Setting
{
    public float ForcedAlpha { get; }
    public float ForcedValue { get; }
    public bool HasForcedAlpha { get; }
    public bool HasForcedValue { get; }
    public ColorSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
        if (accessor.Member.TryGetAttribute<SettingForceColorAlphaAttribute>(out var forcedAlphaAttribute))
        {
            ForcedAlpha = forcedAlphaAttribute.ForcedAlpha;
            HasForcedAlpha = true;
        }
        
        if (accessor.Member.TryGetAttribute<SettingForceColorValueAttribute>(out var forcedValueAttribute))
        {
            ForcedValue = forcedValueAttribute.ForcedValue;
            HasForcedValue = true;
        }
    }

    public override void SetValue(object value)
    {
        var color = (Color)value;

        if (HasForcedAlpha)
        {
            color.A = ForcedAlpha;
        }

        if (HasForcedValue)
        {
            color.ToHsv(out var h, out var s, out var _);
            color = Color.FromHsv(h, s, ForcedValue);
        }
        
        base.SetValue(color);
    }
}
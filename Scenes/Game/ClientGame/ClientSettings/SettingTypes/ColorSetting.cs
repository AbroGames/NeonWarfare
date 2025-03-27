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

    private SettingForceColorAlphaAttribute _forcedAlphaAttribute;
    private SettingForceColorValueAttribute _forcedValueAttribute;
    public ColorSetting(IMemberAccessor accessor, Settings settings) : base(accessor, settings)
    {
        if (accessor.Member.TryGetAttribute<SettingForceColorAlphaAttribute>(out _forcedAlphaAttribute))
        {
            ForcedAlpha = _forcedAlphaAttribute.ForcedAlpha;
            HasForcedAlpha = true;
        }
        
        if (accessor.Member.TryGetAttribute<SettingForceColorValueAttribute>(out _forcedValueAttribute))
        {
            ForcedValue = _forcedValueAttribute.ForcedValue;
            HasForcedValue = true;
        }
    }

    public override void SetValue(object value)
    {
        var color = (Color)value;
        ValidationInfo validationInfo = null;

        if (HasForcedAlpha)
        {
            var preservedColor = color;
            color.A = ForcedAlpha;
            RaiseValueValidationFailed(new (preservedColor, color, _forcedAlphaAttribute));
        }

        if (HasForcedValue)
        {
            var preservedColor = color;
            color.ToHsv(out var h, out var s, out var _);
            color = Color.FromHsv(h, s, ForcedValue);
            RaiseValueValidationFailed(new (preservedColor, color, _forcedValueAttribute));
        }
        
        base.SetValue(color);
    }
}
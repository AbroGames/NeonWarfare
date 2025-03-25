using Godot;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

namespace NeonWarfare.Scripts.Content.Skills;

public static class SkillIconProvider
{
    private const string IconsPath = "res://Assets/Textures/Icons/Abilities/";
    private const string UnknownType = "Unknown";
    
    public static Texture2D GetSkillIcon(string skillType)
    {
        var iconPath = IconsPath + skillType + ".png";
        
        var icon = GD.Load(iconPath) as Texture2D;
        if (!icon.IsValid())
        {
            iconPath = IconsPath + UnknownType + ".png";
            icon = GD.Load(iconPath) as Texture2D;
        }
        
        return icon;
    }
}
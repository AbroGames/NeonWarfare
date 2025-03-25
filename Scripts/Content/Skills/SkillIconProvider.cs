using Godot;

namespace NeonWarfare.Scripts.Content.Skills;

public static class SkillIconProvider
{
    private const string IconsPath = "res://Assets/Textures/Icons/Abilities/";
    private const string UnknownType = "Unknown";
    
    public static Texture2D GetSkillIcon(string skillType)
    {
        var iconPath = IconsPath + skillType + ".png";

        if (!ResourceLoader.Exists(iconPath))
        {
            iconPath = IconsPath + UnknownType + ".png";
        }
        
        var icon = GD.Load<Texture2D>(iconPath);
        return icon;
    }
}
using Godot;

namespace NeonWarfare.Scenes.Root.ClientRoot;

/// <summary>
/// Контейнер для информации о достижении
/// </summary>
public record AchievementData
{
    public string Id { get; init; }
    public Texture2D Icon { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public Color Color { get; init; }
    public bool IsHidden { get; init; }
    
    public AchievementData(string id, Texture2D icon, string name, string description, bool isHidden) : this(id, icon, name, description, Colors.Gold, isHidden) { }

    public AchievementData(string id, Texture2D icon, string name, string description, Color color, bool isHidden)
    {
        Id = id;
        Icon = icon;
        Name = name;
        Description = description;
        Color = color;
        IsHidden = isHidden;
    }
    
    public void Deconstruct(out string id, out Texture2D icon, out string name, out string description, out Color color, out bool isHidden)
    {
        icon = Icon;
        name = Name;
        description = Description;
        color = Color;
        id = Id;
        isHidden = IsHidden;
    }
}
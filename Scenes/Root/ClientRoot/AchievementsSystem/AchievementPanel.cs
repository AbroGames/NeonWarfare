using Godot;
using System;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class AchievementPanel : PanelContainer
{
    [Export] [NotNull] private TextureRect IconRect { get; set; }
    [Export] [NotNull] private Label NameLabel { get; set; }
    [Export] [NotNull] private Label UnlockedLabel { get; set; }

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public void InitializeFrom(AchievementData achievementData)
    {
        NameLabel.Text = achievementData.Name;
        IconRect.Texture = achievementData.Icon;
        NameLabel.Modulate = achievementData.Color;
    }
}

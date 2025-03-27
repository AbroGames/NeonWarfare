using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class AchievementsMenu : Control
{
    [Export] [NotNull] private PackedScene _achievementContainerScene { get; set; }
    [Export] [NotNull] private VBoxContainer _achievementsContainer { get; set; }
    [Export] [NotNull] private Button _returnButton { get; set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        _returnButton.Pressed += () =>
        {
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.MainMenu);
        };

        var achievementsLists = GetAchievementsLists(GetAllAchievements());
        for (var i = 0; i < achievementsLists.Count; i++)
        {
            var list = achievementsLists[i];
            foreach (var achievement in list)
            {
                var container = GetAchievementContainer(achievement);
                _achievementsContainer.AddChild(container);
            }

            if (i != achievementsLists.Count - 1)
            {
                var spacer = new Control();
                var label = new Label();
                var settings = new LabelSettings();
                label.LabelSettings = settings;
                
                spacer.CustomMinimumSize = new Vector2(0, 20);
                settings.FontSize = 24;
                label.Text = "Secret achievements";
                
                _achievementsContainer.AddChild(spacer);
                _achievementsContainer.AddChild(label);
            }
        }
    }

    private AchievementContainer GetAchievementContainer(AchievementData achievement)
    {
        var container = _achievementContainerScene.Instantiate<AchievementContainer>();
        container.InitializeFrom(achievement);
        return container;
    }

    private List<AchievementData> GetAllAchievements()
    {
        return AchievementsList.Achievements.Values.ToList();
    }

    private List<List<AchievementData>> GetAchievementsLists(
        List<AchievementData> allAchievements)
    {
        var visible = allAchievements
            .Where(achievement => !achievement.IsHidden)
            .OrderBy(achievement => achievement.Name)
            .ToList();

        var hidden = allAchievements
            .Where(achievement => achievement.IsHidden)
            .OrderBy(achievement => achievement.Name)
            .ToList();

        return [visible, hidden];
    }
}

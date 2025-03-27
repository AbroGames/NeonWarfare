using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot
{
    [Export] [NotNull] private AchievementsOverlay AchievementsOverlayLayer { get; set; }
    public AchievementsState AchievementsState { get; private set; }

    public bool IsAchievementUnlocked(string achievementId)
    {
        return AchievementsState.IsUnlocked(achievementId);
    }

    public void UnlockAchievement(string achievementId, bool dontSave = false)
    {
        if (IsAchievementUnlocked(achievementId))
            return;
        
        var achievement = AchievementsList.GetAchievement(achievementId);
        AchievementsOverlayLayer.ShowAchievement(achievement);
        
        if (dontSave)
            return;
            
        AchievementsState.UnlockedAchievements.Add(achievementId);
        AchievementsState.Save();
    }
}
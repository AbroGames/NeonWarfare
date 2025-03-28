using Godot;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;

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

        if (Game.IsValid())
        {
            Network.SendToServer(new ServerGame.CS_ClientUnlockedAchievementPacket(achievementId));
        }
        
        if (dontSave)
            return;
            
        AchievementsState.UnlockedAchievements.Add(achievementId);
        AchievementsState.Save();
    }
}
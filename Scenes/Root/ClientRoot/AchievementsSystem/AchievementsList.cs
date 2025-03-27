using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Scenes.Root.ClientRoot;
/// <summary>
/// Список Id, используемых для достижений
/// </summary>
public static class AchievementIds
{
    public const string MainMenuAchievement = "MainMenu";
    public const string BuddyAchievement = "Buddy";
    public const string CrowdAchievement = "Crowd";
    public const string AdvancedSurvivalAchievement = "AdvancedSurvival";
    public const string NopeAchievement = "Nope";
    public const string NinjaAchievement = "Ninja";
    public const string WhereAmIAchievement = "WhereAmI";
    public const string MyBadAchievement = "MyBad";
    public const string Massacre = "Massacre";
    public const string LuckyDevil = "LuckyDevil";
}

/// <summary>
/// Хранилище условий для получения ачивки
/// </summary>
public static class AchievementsPrerequisites
{
    public const int Crowd_PlayersCount = 5;
    public const int AdvancedSurvival_WavesCount = 5;
    public const int WhereAmI_Distance = 50_000;
    public const int Massacre_EnemiesToKill = 100;
    public const double MyBad_TimeToKill = 1.0;
    public const double LuckyDevil_HpLeft = 1.0;
}

/// <summary>
/// Список ачивок
/// </summary>
public static class AchievementsList
{
    
    public static IReadOnlyDictionary<string, AchievementData> Achievements { get; private set; }
    static AchievementsList()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (Achievements is not null)
            return;
        
        var achievements = new Dictionary<string, AchievementData>();
        achievements.AddNew(AchievementIds.MainMenuAchievement, "Main Menu", "You entered the main menu. You lost.");
        achievements.AddNew(AchievementIds.BuddyAchievement, "Buddy!", "Play the game with more than one player in the game world at the same time.");
        achievements.AddNew(AchievementIds.CrowdAchievement, "Crowd", $"Play the game with more than {AchievementsPrerequisites.Crowd_PlayersCount} players in the game world at the same time.");
        achievements.AddNew(AchievementIds.AdvancedSurvivalAchievement, "Advanced Survival Skills", $"Survive {AchievementsPrerequisites.AdvancedSurvival_WavesCount} waves.");
        achievements.AddNew(AchievementIds.NopeAchievement, "Nope", "Join a server where a battle is already in progress.");
        achievements.AddNew(AchievementIds.NinjaAchievement, "Ninja", "Try to set your player color to fully transparent or black in the settings.", isHidden: true);
        achievements.AddNew(AchievementIds.WhereAmIAchievement, "Where Am I?", $"Run {AchievementsPrerequisites.WhereAmI_Distance} pixels away from the world's center.");
        achievements.AddNew(AchievementIds.MyBadAchievement, "Oops, My Bad!", $"Kill a teammate {AchievementsPrerequisites.MyBad_TimeToKill:N0}s after reviving them yourself.", isHidden: true);
        achievements.AddNew(AchievementIds.Massacre, "Massacre", $"Kill {AchievementsPrerequisites.Massacre_EnemiesToKill} enemies in a single game.");
        achievements.AddNew(AchievementIds.LuckyDevil, "Lucky Devil", $"Survive an attack that leaves you with just {AchievementsPrerequisites.LuckyDevil_HpLeft} HP.", isEpic: true);
    
        Achievements = achievements;
    }

    
    
    public static AchievementData GetAchievement(string achievementId)
    {
        return Achievements[achievementId];
    }

    private static void AddNew(this Dictionary<string, AchievementData> achievements, string id, string name, string description, bool isEpic = false, bool isHidden = false)
    {
        var data = MakeData(id, name, description, isEpic, isHidden);
        achievements.Add(id, data);
    }

    private static AchievementData MakeData(string id, string name, string description, bool isEpic = false, bool isHidden = false)
    {
        var icon = GetIcon(id);
        var color = isEpic ? Colors.Purple : Colors.Yellow;
        return new AchievementData(id, icon, name, description, color, isHidden);
    }

    private static Texture2D GetIcon(string id)
    {
        Texture2D icon = null;

        var iconPath = $"res://Assets/Textures/Icons/Achievements/{id}.png";
        var fallbackIconPath = "res://Assets/Textures/Icons/Achievements/DefaultIcon.png";
        
        if (ResourceLoader.Exists(iconPath))
        {
            icon = GD.Load<Texture2D>(iconPath);
        }
        
        icon ??= GD.Load<Texture2D>(fallbackIconPath);

        return icon;
    }
}
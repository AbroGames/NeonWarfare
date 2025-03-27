using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.KludgeBox;
using Newtonsoft.Json;

namespace NeonWarfare.Scenes.Root.ClientRoot;

/// <summary>
/// Класс-хранилище текущего состояния всех ачивок (открыты, закрыты, прогресс)
/// </summary>
public class AchievementsState
{
    private const string AchievementsDataFile = "AchievementsData.json";
    
    /// <summary>
    /// Сюда можно записать прогресс выполнения ачивок, который отслеживается между запусками игры
    /// </summary>
    public Dictionary<string, string> AchievementTrackers { get; private set; } = new ();
    public HashSet<string> UnlockedAchievements { get; private set; } = new();

    public bool IsUnlocked(string achievementId)
    {
        var isUnlocked = UnlockedAchievements.Contains(achievementId);
        return isUnlocked;
    }

    public void Save()
    {
        using var dataFile = FileAccess.Open($"user://{AchievementsDataFile}", FileAccess.ModeFlags.Write);
        try
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            dataFile.StoreString(json);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public static AchievementsState Load()
    {
        try
        {
            using var dataFile = FileAccess.Open($"user://{AchievementsDataFile}", FileAccess.ModeFlags.Read);
            var json = dataFile.GetAsText();
            var state = JsonConvert.DeserializeObject<AchievementsState>(json);
            return state;
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load achievements data: {e.Message}");
            var state = new AchievementsState();
            try
            {
                state.Save();
            }
            catch (Exception exception)
            {
                Log.Warning($"Failed to save new achievements data: {e.Message}");
                throw;
            }

            return state;
        }
    }
}
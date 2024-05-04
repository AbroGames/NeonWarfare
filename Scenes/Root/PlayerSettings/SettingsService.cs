using System;
using System.Text.Json;
using Godot;
using KludgeBox;
using FileAccess = Godot.FileAccess;

namespace NeonWarfare.PlayerSettings;

public static class SettingsService
{
    
    public static void Init()
    {
        var playerSettings = Root.Instance.PlayerSettings;
        try
        {
            FileAccess file = FileAccess.Open($"user://{PlayerSettings.Filename}", FileAccess.ModeFlags.Read);
            string text = file.GetAsText();
            file.Close();
            var data = JsonSerializer.Deserialize<PlayerSettings.SerialisationData>(text);
            playerSettings.PlayerName = data.PlayerName;
            playerSettings.PlayerColor = new Color(data.Red/255f, data.Green/255f, data.Blue/255f, 1);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
        PlayerSettingsSave();
    }

    public static void PlayerSettingsSave()
    {
        var playerSettings = Root.Instance.PlayerSettings;
        try
        {
            if (playerSettings.PlayerName == null || playerSettings.PlayerName.Equals(""))
            {
                playerSettings.PlayerName = "Player";
                playerSettings.PlayerColor = new Color(0, 1, 1, 1);
            }
            var data = new PlayerSettings.SerialisationData(playerSettings);
            
            FileAccess file = FileAccess.Open($"user://{PlayerSettings.Filename}", FileAccess.ModeFlags.WriteRead);
            file.StoreString(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
            file.Close();
        }
        
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
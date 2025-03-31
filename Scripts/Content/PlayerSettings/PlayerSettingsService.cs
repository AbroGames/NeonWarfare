using System;
using System.Text.Json;
using Godot;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content.PlayerSettings;

public static class PlayerSettingsService
{
    public static readonly string Filename = "PlayerSettings.json";
    
    public static Content.PlayerSettings.PlayerSettings LoadSettings()
    {
        try
        {
            FileAccess file = FileAccess.Open($"user://{Filename}", FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            file.Close();
            Content.PlayerSettings.PlayerSettings.SerialisationData serialisationData = JsonSerializer.Deserialize<Content.PlayerSettings.PlayerSettings.SerialisationData>(jsonText);

            return serialisationData.ToPlayerSettings();
        }
        catch (Exception e)
        {
            if (FileAccess.GetOpenError() == Error.FileNotFound)
            {
                Log.Warning("Player settings file not found. Using default settings.");
            }
            else
            {
                Log.Error($"Failed to load player settings: {FileAccess.GetOpenError()}. Using default settings.");
                Log.Error(e);
            }
            
            Content.PlayerSettings.PlayerSettings defaultSettings = new Content.PlayerSettings.PlayerSettings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }
    }
	
    public static void SaveSettings(Content.PlayerSettings.PlayerSettings playerSettings)
    {
        try
        {
            Content.PlayerSettings.PlayerSettings.SerialisationData data = new Content.PlayerSettings.PlayerSettings.SerialisationData(playerSettings);
            string jsonText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
                
            FileAccess file = FileAccess.Open($"user://{Filename}", FileAccess.ModeFlags.WriteRead);
            file.StoreString(jsonText);
            file.Close();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}

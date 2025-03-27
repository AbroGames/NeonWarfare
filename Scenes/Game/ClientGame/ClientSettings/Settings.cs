using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.Content.PlayerSettings;
using Newtonsoft.Json;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings;

public class Settings
{
    public static event Action<Settings> Changed;

    [SettingName("Player Settings")]
    [SettingDescription("Settings related to the visual and informational aspects of the player in a networked game.")]
    public SettingGroup PlayerSettingsGroup { get; set; }

    [SettingName("Player Name")]
    [SettingDescription("The player's name used in the context of the networked game.")]
    public string PlayerName { get; set; } = "Player";

    [SettingName("Player Color")]
    [SettingDescription("Determines the color of the player's character in the game.")]
    [SettingForceColorAlpha(1)]
    [SettingForceColorValue(1)]
    public Color PlayerColor { get; set; } = Colors.Green;


    [SettingName("Audio Settings")]
    [SettingDescription("Settings for adjusting the volume of sound effects, music, and audio overall.")]
    public SettingGroup AudioSettingsGroup { get; set; }

    [SettingName("Master Volume")]
    [SettingDescription("The overall volume level in the game. Affects all sound in the game.")]
    [SettingNumberRange(0, 100)]
    [SettingNumberStep(1)]
    [SettingNumberInputType(NumberInputType.Slider)]
    public float MasterVolume { get; set; } = 100;

    [SettingName("Sound Effects Volume")]
    [SettingDescription("Adjusts the volume of sound effects.")]
    [SettingNumberRange(0, 100)]
    [SettingNumberStep(1)]
    [SettingNumberInputType(NumberInputType.Slider)]
    public float SoundVolume { get; set; } = 100;

    [SettingName("Music Volume")]
    [SettingDescription("Adjusts the volume of in-game music.")]
    [SettingNumberRange(0, 100)]
    [SettingNumberStep(1)]
    [SettingNumberInputType(NumberInputType.Slider)]
    public float MusicVolume { get; set; } = 100;


    [SettingName("Game Settings")]
    [SettingDescription("Additional settings that modify the game’s behavior.")]
    public SettingGroup GameSettingsGroup { get; set; }

    [SettingName("Maximize on Start")]
    [SettingDescription("Maximizes the game window to full screen upon launch.")]
    public bool MaximizeOnStart { get; set; } = false;
    
    
    
    internal static void InvokeChanged(Settings settings) => Changed?.Invoke(settings);
    
    public PlayerSettings GetPlayerSettings()
    {
        return new PlayerSettings(
            PlayerName,
            PlayerColor);
    }
}

internal class TestingOptionsProvider : IOptionsProvider
{
    public IReadOnlyList<string> GetOptions()
    {
        return ["Option D", "Option E", "Option F"];
    }
}

public struct SettingGroup;
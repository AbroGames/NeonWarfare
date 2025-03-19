using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.Utils.PlayerSettings;
using Newtonsoft.Json;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings;

public class Settings
{
    public static event Action<Settings> Changed;

    [SettingName("Настройки игрока")]
    [SettingDescription("Настройки отвечающие за визуальную и информативную составляющую игрока в сетевой игре.")]
    public SettingGroup PlayerSettingsGroup { get; set; }
    
    [SettingName("Имя игрока")]
    [SettingDescription("Имя игрока, используемое в контексте сетевой игры.")]
    public string PlayerName { get; set; } = "Player";
    
    [SettingName("Цвет игрока")]
    [SettingDescription("Определяет цвет персонажа игрока в игре.")]
    [SettingForceColorAlpha(1)]
    public Color PlayerColor { get; set; } = Colors.Green;
    
    
    
    [SettingName("Настройки аудио")]
    [SettingDescription("Настройки громкости звуковых эффектов музыки и аудио в целом.")]
    public SettingGroup AudioSettingsGroup { get; set; }
    
    [SettingName("Общая громкость")]
    [SettingDescription("Общая громкость звука в игре. Влияет на вообще весь звук в игре.")]
    [SettingNumberRange(0, 100)]
    [SettingNumberStep(1)]
    [SettingNumberInputType(NumberInputType.Slider)]
    public float MasterVolume { get; set; } = 100;

    [SettingName("Громкость звуков")]
    [SettingDescription("Регулирует громкость звуковых эффектов.")]
    [SettingNumberRange(0, 100)]
    [SettingNumberStep(1)]
    [SettingNumberInputType(NumberInputType.Slider)]
    public float SoundVolume { get; set; } = 100;

    [SettingName("Громкость музыки")]
    [SettingDescription("Регулирует громкость музыки в игре.")]
    [SettingNumberRange(0, 100)]
    [SettingNumberStep(1)]
    [SettingNumberInputType(NumberInputType.Slider)]
    public float MusicVolume { get; set; } = 100;
    
    
    
    [SettingName("Настройки игры")]
    [SettingDescription("Дополнительные настройки, изменяющие поведение игры.")]
    public SettingGroup GameSettingsGroup { get; set; }

    [SettingName("Развернуть при запуске")]
    [SettingDescription("Развернет окно игры на весь экран при запуске.")]
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
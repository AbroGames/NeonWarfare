namespace NeonWarfare.Scripts.Service.Settings;

public interface IPlayerSettingsService
{
    void Init();
    PlayerSettings GetPlayerSettings();
    void SetPlayerSettings(PlayerSettings playerSettings);
    void SetNickTemporarily(string nick);
}
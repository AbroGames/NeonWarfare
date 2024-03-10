using System;
using System.IO;
using System.Text.Json;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class SettingsService
{
    
    [EventListener]
    public void OnSettingsInitRequest(SettingsInitRequest r)
    {
        var playerInfo = Root.Instance.Game.PlayerInfo;
        try
        {
            string text = File.ReadAllText(PlayerInfo.Filename);
            var data = JsonSerializer.Deserialize<PlayerInfo.SerialisationData>(text);
            playerInfo.PlayerName = data.PlayerName;
            playerInfo.PlayerColor = new Color(data.Red/255f, data.Green/255f, data.Blue/255f, 1);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
        EventBus.Publish(new PlayerInfoSaveEvent());
    }

    [EventListener]
    public void OnPlayerInfoSaveEvent(PlayerInfoSaveEvent playerInfoSaveEvent)
    {
        var playerInfo = Root.Instance.Game.PlayerInfo;
        try
        {
            if (playerInfo.PlayerName == null || playerInfo.PlayerName.Equals(""))
            {
                playerInfo.PlayerName = "Player";
                playerInfo.PlayerColor = new Color(0, 1, 1, 1);
            }
            var data = new PlayerInfo.SerialisationData(playerInfo);
            if (!File.Exists(PlayerInfo.Filename))
                File.Create(PlayerInfo.Filename);
            File.WriteAllText(PlayerInfo.Filename,
                JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
        }
        
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
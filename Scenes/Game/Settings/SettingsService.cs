using System;
using System.IO;
using System.Text.Json;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using FileAccess = Godot.FileAccess;

namespace NeoVector;

[GameService]
public class SettingsService
{
    
    [EventListener]
    public void OnSettingsInitRequest(SettingsInitRequest r)
    {
        var playerInfo = Root.Instance.PlayerInfo;
        try
        {
            FileAccess file = FileAccess.Open($"user://{PlayerInfo.Filename}", FileAccess.ModeFlags.Read);
            string text = file.GetAsText();
            file.Close();
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
        var playerInfo = Root.Instance.PlayerInfo;
        try
        {
            if (playerInfo.PlayerName == null || playerInfo.PlayerName.Equals(""))
            {
                playerInfo.PlayerName = "Player";
                playerInfo.PlayerColor = new Color(0, 1, 1, 1);
            }
            var data = new PlayerInfo.SerialisationData(playerInfo);
            
            FileAccess file = FileAccess.Open($"user://{PlayerInfo.Filename}", FileAccess.ModeFlags.WriteRead);
            file.StoreString(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
            file.Close();
        }
        
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
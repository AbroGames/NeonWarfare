using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class SettingsService
{
    [EventListener]
    public void OnSettingsInitRequest(SettingsInitRequest r)
    {
        var playerInfo = Root.Instance.Game.PlayerInfo;
        playerInfo.PlayerName = "Player";
        playerInfo.PlayerColor = new Color(0, 1, 1, 1);
    }
}
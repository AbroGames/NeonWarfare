using Godot;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Screen.SafeHud;

public partial class ReadyToBattleButton : Button
{
    public override void _Ready()
    {
        Toggled += (doWant) =>
        {
            Network.SendToServer(new ServerGame.CS_WantToBattlePacket(doWant));
        };
    }

}

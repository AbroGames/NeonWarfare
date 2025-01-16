using Godot;
using KludgeBox.Networking;

namespace NeonWarfare.Scenes.Screen.SafeHud;

public partial class ToBattleButton : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            Network.SendToServer(new ServerGame.CS_WantToBattlePacket());
        };
    }

}

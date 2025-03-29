using Godot;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Screen.SafeHud;

public partial class ToBattleButton : Button
{
    public override void _Ready()
    {
        Visible = ClientRoot.Instance.Game.PlayerProfile.IsAdmin;
        Pressed += () =>
        {
            Network.SendToServer(new ServerGame.CS_AdminGoToBattlePacket());
        };
    }

}

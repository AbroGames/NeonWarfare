using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.PlayerSettingsInterface;

public partial class NickLineEdit : LineEdit
{
    public override void _Ready()
    {
        //Text = ClientRoot.Instance.PlayerSettings.PlayerName;
    }
}

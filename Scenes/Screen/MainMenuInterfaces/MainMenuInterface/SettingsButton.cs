using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.MainMenuInterface;

public partial class SettingsButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        if (!ClientRoot.Instance.CmdParams.IsHelper)
        {
            Pressed += () =>
            {
                MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.SettingsMenu);
            };
        }
        else
        {
            Disabled = true;
            TooltipText = "Settings menu are not available in helper mode";
        }
    }
}

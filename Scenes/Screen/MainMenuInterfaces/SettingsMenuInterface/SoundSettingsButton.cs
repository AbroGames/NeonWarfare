using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.SettingsMenuInterface;

public partial class SoundSettingsButton : Button
{
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Pressed += () =>
		{
			MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.SoundSettingsMenu);
		};
	}
}

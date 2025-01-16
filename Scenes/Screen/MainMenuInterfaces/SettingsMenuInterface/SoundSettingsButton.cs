using Godot;
using KludgeBox;

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

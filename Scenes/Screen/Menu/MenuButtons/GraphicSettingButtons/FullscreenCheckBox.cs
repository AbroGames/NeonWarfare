using Godot;

namespace NeonWarfare;
public partial class FullscreenCheckBox : CheckBox
{
	public override void _Ready()
	{
		this.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
		Pressed += () =>
		{
			if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			else
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		};
	}
}

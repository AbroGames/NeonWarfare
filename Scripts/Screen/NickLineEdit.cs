using Godot;

namespace KludgeBox.Events.Global;

public partial class NickLineEdit : LineEdit
{
	public override void _Ready()
	{
		Text = Root.Instance.Game.PlayerInfo.playerName;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
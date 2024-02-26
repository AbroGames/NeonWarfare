using Godot;

namespace AbroDraft;

public partial class PlayerInfo : Node2D
{
	public string playerName;
	public Color playerColor;
	
	public override void _Ready()
	{
		playerName = "Player";
		playerColor = new Color();
	}
}
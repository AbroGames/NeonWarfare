using System.Text.Json.Serialization;
using Godot;

namespace NeonWarfare;

public partial class PlayerSettings : Node2D
{
	public static readonly string Filename = "PlayerSettings.json";
	public string PlayerName;
	public Color PlayerColor;

	[method: JsonConstructor]
	public class SerialisationData(string playerName, int red, int green, int blue)
	{
		public SerialisationData(PlayerSettings playerSettings) : this(playerSettings.PlayerName, (int) (playerSettings.PlayerColor.R * 255f), (int) (playerSettings.PlayerColor.G * 255f), (int) (playerSettings.PlayerColor.B * 255f))
		{
		}

		public string PlayerName { get; set; } = playerName;
		public int Red { get; set; } = red;
		public int Green { get; set; } = green;
		public int Blue { get; set; } = blue;
	}
	public override void _Ready()
	{
		
	}
}
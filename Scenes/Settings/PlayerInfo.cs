using System;
using System.Text.Json.Serialization;
using Godot;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class PlayerInfo : Node2D
{
	public static readonly string Filename = "PlayerSettings.json";
	public string PlayerName;
	public Color PlayerColor;

	[method: JsonConstructor]
	public class SerialisationData(string playerName, int red, int green, int blue)
	{
		public SerialisationData(PlayerInfo playerInfo) : this(playerInfo.PlayerName, (int) (playerInfo.PlayerColor.R * 255f), (int) (playerInfo.PlayerColor.G * 255f), (int) (playerInfo.PlayerColor.B * 255f))
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
using System;
using System.Text.Json;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public class PlayerSettings
{

	//Здесь задаем дефолтные настройки игрока, которые будут использоваться при первом входе в игру
	public string PlayerName = "Player";
	public Color PlayerColor = new(0, 1, 1);
	
	public PlayerSettings() {}

	private PlayerSettings(string playerName, Color playerColor)
	{
		PlayerName = playerName;
		PlayerColor = playerColor;
	}

	[method: System.Text.Json.Serialization.JsonConstructor]
	public class SerialisationData(string playerName, int red, int green, int blue)
	{
		public SerialisationData(PlayerSettings playerSettings) : this(
			playerSettings.PlayerName, 
			(int) (playerSettings.PlayerColor.R * 255f), 
			(int) (playerSettings.PlayerColor.G * 255f), 
			(int) (playerSettings.PlayerColor.B * 255f)
			) { }

		public string PlayerName { get; set; } = playerName;
		public int Red { get; set; } = red;
		public int Green { get; set; } = green;
		public int Blue { get; set; } = blue;

		public PlayerSettings ToPlayerSettings()
		{
			return new PlayerSettings(
				PlayerName,
				new Color(Red/255f, Green/255f, Blue/255f, 1));
		}
	}
}
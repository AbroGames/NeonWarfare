using System;
using System.Text.Json;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class PlayerSettings : Node
{
	public static readonly string Filename = "PlayerSettings.json";
	
	public string PlayerName;
	public Color PlayerColor;

	[method: System.Text.Json.Serialization.JsonConstructor]
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
	
	public void Init()
	{
		try
		{
			FileAccess file = FileAccess.Open($"user://{Filename}", FileAccess.ModeFlags.Read);
			string text = file.GetAsText();
			file.Close();
			var data = JsonSerializer.Deserialize<SerialisationData>(text);
			PlayerName = data.PlayerName;
			PlayerColor = new Color(data.Red/255f, data.Green/255f, data.Blue/255f, 1);
		}
		catch (Exception e)
		{
			Log.Error(e);
		}
		
		Save();
	}
	
	public void Save()
	{
		try
		{
			if (PlayerName == null || PlayerName.Equals(""))
			{
				PlayerName = "Player";
				PlayerColor = new Color(0, 1, 1, 1);
			}
			var data = new SerialisationData(this);
            
			FileAccess file = FileAccess.Open($"user://{Filename}", FileAccess.ModeFlags.WriteRead);
			file.StoreString(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
			file.Close();
		}
        
		catch (Exception e)
		{
			Log.Error(e);
		}
	}
}
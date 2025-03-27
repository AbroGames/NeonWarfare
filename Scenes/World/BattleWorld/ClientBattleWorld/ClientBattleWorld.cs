using System.Linq;
using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;

namespace NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;

public partial class ClientBattleWorld : ClientWorld
{
	public int CurrentWave { get; set; }
	public double TimeToWave { get; set; }
	
	public override void _Ready()
	{
		base._Ready();
        PlayBattleMusic();
	}

	public override void _Process(double delta)
	{
		if (TimeToWave > 0)
		{
			TimeToWave = Mathf.Max(0, TimeToWave - delta);
		}
	}
}

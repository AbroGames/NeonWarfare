using System.Collections.Generic;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeonWarfare;

public partial class ClientBattleWorld : ClientWorld
{
	public BattleHud BattleHud { get; set; }
	public EnemyWave EnemyWave { get; set; } = new(); //TODO it is component, rename it
	public readonly ISet<Enemy> Enemies = new HashSet<Enemy>();
	public readonly ISet<Enemy> EnemyAttractors = new HashSet<Enemy>();
	
	public override void _Ready()
	{
		base._Ready();
        
		/*battleWorld.Player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
		battleWorld.Player.Position = Vec(500, 500);

		var camera = new Camera(); //TODO to camera service/component
		camera.Position = battleWorld.Player.Position;
		camera.TargetNode = battleWorld.Player;
		camera.Zoom = Vec(0.3);
		camera.SmoothingPower = 1.5;
		battleWorld.AddChild(camera);
		camera.Enabled = true;

		var floor = battleWorld.Floor;
		floor.Camera = camera;
		floor.ForceCheck();

		battleWorld.AddChild(battleWorld.Player); // must be here to draw over the floor*/
        
        
		if (Rand.Chance(0.5))//TODO to music service (battle music service)
		{
			PlayBattleMusic1();
		}
		else
		{
			PlayBattleMusic2();
		}
	}
	
	private void PlayBattleMusic1()
	{
		var music = Audio2D.PlayMusic(Music.WorldBgm1, 0.5f);
		music.Finished += PlayBattleMusic2;
	}
    
	private void PlayBattleMusic2()
	{
		var music = Audio2D.PlayMusic(Music.WorldBgm2, 0.5f);
		music.Finished += PlayBattleMusic1;
	}
}
using Godot;
using System;
using System.Collections.Generic;
using Game.Content;
using KludgeBox;

public partial class BattleWorld : Node2D
{

	private const int OneWaveEnemyCount = 15; 
	private const int OneWaveEnemyCountDelta = 5; 
	private const int WaveTimeout = 12;

	public Player Player;
	public readonly ISet<Enemy> Enemies = new HashSet<Enemy>();
	public int WaveNumber = 0;
	
	private double _nextWaveTimer = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player = Root.Instance.PackedScenes.World.Player.Instantiate() as Player;
		Player.Position = Vec(500, 500);
		
		var camera = new PlayerCamera();
		camera.Position = Player.Position;
		camera.TargetNode = Player;
		camera.Zoom = Vec(0.65);
		camera.SmoothingPower = 1.5;
		AddChild(camera);
		camera.Enabled = true;

		var floor = new Floor();
		floor.Camera = camera;
		AddChild(floor);
		
		var ally = Root.Instance.PackedScenes.World.Ally.Instantiate() as Node2D;
		ally.Position = Vec(600, 600);
		AddChild(ally);
		AddChild(Player); // must be here to draw over the floor
		Audio2D.PlayMusic(Music.WorldBgm);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		SpawnWave(delta);
	}

	private void SpawnWave(double delta)
	{
		_nextWaveTimer -= delta;
		if (_nextWaveTimer > 0)
		{
			return;
		}
		_nextWaveTimer = WaveTimeout;
		WaveNumber++;
		
		for (int i = 0; i < OneWaveEnemyCount + WaveNumber * OneWaveEnemyCountDelta; i++)
		{
			CreateEnemyAroundCharacter(Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
		}

		if (WaveNumber % 5 == 0)
		{
			for (int i = 0; i < WaveNumber / 5; i++)
			{
				CreateBossEnemyAroundCharacter(Player, Rand.Double * Mathf.Pi * 2, Rand.Range(2600, 3000));
			}
		}


		Audio2D.PlayUiSound(Sfx.Bass, 0.1f); // dat bass on start
	}

	private void CreateEnemyAroundCharacter(Character character, double angle, double distance)
	{
		var enemy = genEnemyAroundCharacter(character, angle, distance);
		AddChild(enemy);
		Enemies.Add(enemy);
	}
	
	private void CreateBossEnemyAroundCharacter(Character character, double angle, double distance)
	{
		var enemy = genEnemyAroundCharacter(character, angle, distance);
		var scale = 1 + 0.1 * WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
		enemy.Transform = enemy.Transform.ScaledLocal(Vec(scale));  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
		enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
		enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
		enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
		enemy.BaseXp *= (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
		AddChild(enemy);
		Enemies.Add(enemy);
	}

	private Enemy genEnemyAroundCharacter(Character character, double angle, double distance)
	{
		var targetPositionDelta = Vector2.FromAngle(angle) * distance;
		var targetPosition = character.Position + targetPositionDelta;
			
		var enemy = Root.Instance.PackedScenes.World.Enemy.Instantiate() as Enemy;
		enemy.Position = targetPosition;
		enemy.Rotation = angle - Math.PI / 2;
		enemy.Target = character;
		enemy.Hp = 250;
		enemy.MovementSpeed = 200; // in pixels/sec
		return enemy;
	}
}

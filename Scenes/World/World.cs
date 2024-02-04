using Godot;
using System;
using Game.Content;
using KludgeBox;

public partial class World : Node2D
{

	private const int OneWaveEnemyCount = 25; 
	private const int WaveTimeout = 5; 
	
	private Character _character;
	private double _nextWaveTimer = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_character = Root.Instance.PackedScenes.World.Character.Instantiate() as Character;
		_character.Position = Vec(500, 500);
		
		var camera = new PlayerCamera();
		camera.Position = _character.Position;
		camera.TargetNode = _character;
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
		AddChild(_character); // must be here to draw over the floor
		
		Audio2D.PlaySoundAt(Sfx.Bass, _character.Position, 0.1f); // dat bass on start
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
		
		for (int i = 0; i < OneWaveEnemyCount; i++)
		{
			CreateEnemyRandomPosAroundCharacter(_character);
		}
	}

	private void CreateEnemyRandomPosAroundCharacter(Character character)
	{
		CreateEnemyAroundCharacter(character, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
	}

	private void CreateEnemyAroundCharacter(Character character, double angle, double distance)
	{
		var targetPositionDelta = Vector2.FromAngle(angle) * distance;
		var targetPosition = character.Position + targetPositionDelta;
			
		var enemy = Root.Instance.PackedScenes.World.Enemy.Instantiate() as Enemy;
		enemy.Position = targetPosition;
		enemy.Rotation = angle - Math.PI / 2;
		enemy.Target = character as Character;
		AddChild(enemy);
	}
}

using Godot;
using System;
using AbroDraft.WorldEntities;
using KludgeBox;

public partial class World : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var character = References.Instance.CharacterBlueprint.Instantiate() as Node2D;
		character.Position = Vec(500, 500);
		AddChild(character);
		
		var camera = new PlayerCamera();
		camera.Position = character.Position;
		camera.TargetNode = character;
		camera.Zoom = Vec(0.65);
		AddChild(camera);
		camera.Enabled = true;
		
		var ally = References.Instance.AllyBlueprint.Instantiate() as Node2D;
		ally.Position = Vec(600, 600);
		AddChild(ally);

		for (int i = 0; i < 25; i++)
		{
			CreateEnemyRandomPosAroundCharacter(character as Character);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void CreateEnemyRandomPosAroundCharacter(Character character)
	{
		CreateEnemyAroundCharacter(character, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
	}

	private void CreateEnemyAroundCharacter(Character character, double angle, double distance)
	{
		var targetPositionDelta = Vector2.FromAngle(angle) * distance;
		var targetPosition = character.Position + targetPositionDelta;
			
		var enemy = References.Instance.EnemyBlueprint.Instantiate() as Enemy;
		enemy.Position = targetPosition;
		enemy.Rotation = angle - Math.PI / 2;
		enemy.Target = character as Character;
		AddChild(enemy);
	}
}

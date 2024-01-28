using System;
using Game.Content;
using Godot;
using MicroSurvivors;

public partial class Ally : CharacterBody2D
{
	
	[Export] private PackedScene _bulletBlueprint;
	
	public void Attack(double rotation)
	{
		// Создание снаряда
		Bullet bullet = _bulletBlueprint.Instantiate() as Bullet;
		// Установка начальной позиции снаряда
		bullet.GlobalPosition = GlobalPosition;
		// Установка направления движения снаряда
		bullet.Rotation = rotation;
		bullet.Author = Bullet.AuthorEnum.ALLY;
		
		GetParent().AddChild(bullet);
	}
}
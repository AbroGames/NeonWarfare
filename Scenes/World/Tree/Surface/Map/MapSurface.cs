using Godot;
using NeonWarfare.Scenes.NeonTemp;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

namespace NeonWarfare.Scenes.World.Tree.Surface.Map;

public partial class MapSurface : Node2D
{
    public override void _Ready()
    {
        //ReadyPhysicTest();
        ReadyCharacterTest();
    }

    public void ReadyPhysicTest()
    {
        AddPhysicCharacter(250, 250, Vec2(1, 0));
        AddPhysicCharacter(350, 250, Vec2(-1, 0));
        AddPhysicCharacter(450, 250, Vec2(-1, 0));
        
        CharacterPhysicsTest player = AddPhysicCharacter(0, 250, Vec2(0, 0));
        player.PlayerController = new PlayerController();
        player.Mass = 1;
        player.Acceleration = 25;
        
        AddWall(300, 300);
        AddWall(350, 350);
        AddWall(400, 400).Scale = Vec2(1, 4);
    }
    
    public void ReadyCharacterTest()
    {
        Character player = AddCharacter(250, 250);
        //player.PlayerController = new PlayerController();
        
        AddWall(300, 300);
        AddWall(350, 350);
        AddWall(400, 400).Scale = Vec2(1, 4);
    }

    public CharacterPhysicsTest AddPhysicCharacter(float x, float y, Vector2 vec)
    {
        PackedScene characterPs = GD.Load<PackedScene>("res://Scenes/NeonTemp/Entity/Character/CharacterPhysicsTest.tscn");
        CharacterPhysicsTest character = characterPs.Instantiate<CharacterPhysicsTest>();
        character.Position = Vec2(x, y);
        character.Vec = vec;
        this.AddChildWithUniqueName(character, "PhysicCharacter");
        return character;
    }
    
    public Character AddCharacter(float x, float y)
    {
        PackedScene characterPs = GD.Load<PackedScene>("res://Scenes/NeonTemp/Entity/Character/Character.tscn");
        Character character = characterPs.Instantiate<Character>();
        character.Position = Vec2(x, y);
        this.AddChildWithUniqueName(character, "Character");
        return character;
    }
    
    public Node2D AddWall(float x, float y)
    {
        PackedScene wallPs = GD.Load<PackedScene>("res://Scenes/NeonTemp/Entity/Wall/Wall.tscn");
        Node2D wall = wallPs.Instantiate<Node2D>();
        wall.Position = Vec2(x, y);
        this.AddChildWithUniqueName(wall, "Wall");
        return wall;
    }
}
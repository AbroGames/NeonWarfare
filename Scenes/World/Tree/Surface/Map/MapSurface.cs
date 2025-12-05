using Godot;
using KludgeBox.Core.Stats;
using KludgeBox.DI.Requests.ParentInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Remote;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

namespace NeonWarfare.Scenes.World.Tree.Surface.Map;

public partial class MapSurface : Node2D
{
    [Parent(true)] private World _world;
    
    public override void _Ready()
    {
        Di.Process(this);
        ReadyPhysicTest();
        
        AddWall(300, 300);
        AddWall(350, 350);
        AddWall(400, 400).Scale = Vec2(1, 4);
    }
    
    public Node2D AddWall(float x, float y)
    {
        PackedScene wallPs = GD.Load<PackedScene>("res://Scenes/NeonTemp/Entity/Wall/Wall.tscn");
        Node2D wall = wallPs.Instantiate<Node2D>();
        wall.Position = Vec2(x, y);
        this.AddChildWithUniqueName(wall, "Wall");
        return wall;
    }
    
    // --------------------------------------------------------

    public void ReadyPhysicTest()
    {
        AddPhysicCharacter(250, 250, Vec2(1, 0));
        AddPhysicCharacter(350, 250, Vec2(-1, 0));
        AddPhysicCharacter(450, 250, Vec2(-1, 0));
        
        CharacterPhysicsTest player = AddPhysicCharacter(0, 450, Vec2(0, 0));
        player.Controlled = true;
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
    
    // --------------------------------------------------------
    
    public void AddPlayerCharacter(long peerId)
    {
        Character player = AddCharacter(250, 250);
        player.Controller.SetController(new RemoteController());
        player.Controller.SetControllerToClient(peerId, new PlayerController());
        
        Character bot = AddCharacter(450, 250);
    }
    
    public Character AddCharacter(float x, float y)
    {
        PackedScene characterPs = GD.Load<PackedScene>("res://Scenes/NeonTemp/Entity/Character/Character.tscn");
        Character character = characterPs.Instantiate<Character>();
        character.Position = Vec2(x, y);
        this.AddChildWithUniqueName(character, "Character");
        
        character.Controller.Teleport(character.Position);
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.MovementSpeed, 200));
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.RotationSpeed, 360));
        return character;
    }
}
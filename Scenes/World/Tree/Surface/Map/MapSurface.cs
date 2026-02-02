using System.Collections.Generic;
using Godot;
using KludgeBox.Core.Stats;
using KludgeBox.DI.Requests.ParentInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;
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
        //ReadyPhysicTest();
        
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

        CharacterPhysicsTest bot0 = AddPhysicCharacter(700, 200, Vec2(0, 0));
        //bot0.LogId = "Bot0";
        
        CharacterPhysicsTest bot1 = AddPhysicCharacter(700, 250, Vec2(0, 0));
        bot1.Mass *= 5; //bot1.LogId = "Bot1";
        CharacterPhysicsTest bot2 = AddPhysicCharacter(700, 300, Vec2(0, 0));
        bot2.Mass /= 5;
        CharacterPhysicsTest bot3 = AddPhysicCharacter(700, 350, Vec2(0, 0));
        bot3.Force *= 5;
        CharacterPhysicsTest bot4 = AddPhysicCharacter(700, 400, Vec2(0, 0));
        bot4.Force /= 5;
        CharacterPhysicsTest bot5 = AddPhysicCharacter(700, 450, Vec2(0, 0));
        bot5.Mass *= 5; bot5.Force *= 5;
        CharacterPhysicsTest bot6 = AddPhysicCharacter(700, 500, Vec2(0, 0));
        bot6.Mass /= 5; bot6.Force /= 5;
        CharacterPhysicsTest bot7 = AddPhysicCharacter(700, 550, Vec2(0, 0));
        bot7.Mass *= 5; bot7.Force /= 5;
        CharacterPhysicsTest bot8 = AddPhysicCharacter(700, 600, Vec2(0, 0));
        bot8.Mass /= 5; bot8.Force *= 5;
        
        
        CharacterPhysicsTest player = AddPhysicCharacter(0, 450, Vec2(0, 0));
        player.Controlled = true;
        player.Mass *= 1;
        player.LogId = "Player";
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
        bot.Controller.SetController(new AiController(new AiObserveControllerLogic()));
    }
    
    public Character AddCharacter(float x, float y)
    {
        PackedScene characterPs = GD.Load<PackedScene>("res://Scenes/NeonTemp/Entity/Character/Character.tscn");
        Character character = characterPs.Instantiate<Character>();
        character.Position = Vec2(x, y);
        this.AddChildWithUniqueName(character, "Character");
        
        
        // Телепорт необходим, чтобы клиент при спауне не отправил на сервер координаты (0;0) через контроллер и не сбрасывал позицию на сервере
        // Отправка координат (0;0) происходит из-за того, что для Character отключен синк Position в MultiplayerSynchronizer
        // Для ботов это позволяет избежать рывка с коориднат (0;0) при спавне
        
        //TODO Сейчас дергается бот на клиенте при спавне! Он летит из (0;0). Надо либо вернуть обратно эту функцию на просто установку Character.Position,
        //TODO либо подебажить как в обоих реализациях работает телепорт по среди игры, а не только в момент спауна (для этого на кнопку Тест 1 ищет Character игрока/бота в мире и телепортируем его на фиксированную позицию) 
        //TODO Подсказка: игрок имеет имя Character-1-3, а бот Character-1-4
        character.Controller.Teleport(character.Position);
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.MovementSpeed, 200));
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.RotationSpeed, 360));
        return character;
    }
}
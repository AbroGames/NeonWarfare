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
        //AddBotCharacter(250, 250, Vec2(1, 0));
        //AddBotCharacter(350, 250, Vec2(-1, 0));
        //AddBotCharacter(450, 250, Vec2(-1, 0));

        Character bot0 = AddBotCharacter(700, 200);
        //bot0.LogId = "Bot0";
        
        Character bot1 = AddBotCharacter(700, 250);
        bot1.Mass *= 5; //bot1.LogId = "Bot1";
        Character bot2 = AddBotCharacter(700, 300);
        bot2.Mass /= 5;
        Character bot3 = AddBotCharacter(700, 350);
        bot3.Controller.ForceCoef *= 5;
        Character bot4 = AddBotCharacter(700, 400);
        bot4.Controller.ForceCoef /= 5;
        Character bot5 = AddBotCharacter(700, 450);
        bot5.Mass *= 5; bot5.Controller.ForceCoef *= 5;
        Character bot6 = AddBotCharacter(700, 500);
        bot6.Mass /= 5; bot6.Controller.ForceCoef /= 5;
        Character bot7 = AddBotCharacter(700, 550);
        bot7.Mass *= 5; bot7.Controller.ForceCoef /= 5;
        Character bot8 = AddBotCharacter(700, 600);
        bot8.Mass /= 5; bot8.Controller.ForceCoef *= 5;
    }
    
    // --------------------------------------------------------
    
    private bool _botsWasCreated = false;
    public void AddPlayerCharacter(long peerId)
    {
        if (!_botsWasCreated)
        {
            ReadyPhysicTest();
            _botsWasCreated = true;
        }
        
        Character player = AddCharacter(250, 250);
        //TODO В синглплеерной игре порядок имеет значение?
        player.Controller.SetController(new RemoteController());
        player.Controller.SetControllerToClient(peerId, new PlayerController());
        
        //Character bot = AddCharacter(450, 250);
        //bot.Controller.SetController(new AiController(new AiObserveControllerLogic()));
    }

    public Character AddBotCharacter(float x, float y, IAiControllerLogic aiLogic = null)
    {
        Character bot = AddCharacter(x, y);
        bot.Controller.SetController(new AiController(aiLogic ?? new AiBattleControllerLogic()));
        return bot;
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
        
        //TODO В доку: для избежания появления юнита на кадр в корах 0;0 и для следов (интерполяции) при телепорте, мы в Ready отключаем интерполяцию на 1 кадр, а Position синхроним через MpSync при спавне
        //TODO Но это +1 нода, так что возможно стоит спавнить юнитов, передавая коры при спавне, через RPC (но тогда мы не увидим других игроков при подключении??!)
        //TODO или через MpSpawner.SpawnFunction + MpSpawner.Spawn в byte[] через сериализатор (протестить как работает синк при подключении игрока по ходу игры), код тут https://gemini.google.com/app/21c0306cc9a7fccb
        //character.Controller.Teleport(character.Position);
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.MovementSpeed, 200));
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.RotationSpeed, 360));
        return character;
    }
}
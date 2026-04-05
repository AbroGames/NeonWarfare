using Godot;
using KludgeBox.Core.Stats;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Remote;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Tree;

namespace NeonWarfare.Scenes.World.Service.Characters;

public partial class WorldCharacterService : Node
{
    [SceneService] protected WorldTree Tree;
    [SceneService] protected WorldPersistenceData PersistenceData;
    [SceneService] protected WorldTemporaryData TemporaryData;
    
    [SceneService] protected SyncedPackedScenes SyncedPackedScenes;
    
    public override void _Ready()
    {
        Di.Process(this);
    }
    
    public void AddPlayerCharacter(long peerId)
    {
        
        Character player = AddCharacter(250, 250);
        //TODO В синглплеерной игре порядок имеет значение?
        player.Controller.SetController(new RemoteController());
        player.Controller.SetControllerToClient(peerId, new PlayerController());

        player.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.MaxHp, 100));
        player.Stats.Heal(player, player.Stats.MaxHp);
        //var effect = new PoisonStatusEffect.Builder().Id("Poison").Time(100).PoisonTime(1).PoisonValue(10).Build();
        //player.StatusEffects.AddStatusEffect(effect, player);
        
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
        Character character = SyncedPackedScenes.Character.Instantiate<Character>();
        character.Position = Vec2(x, y);
        Tree.Surface.AddChildWithUniqueName(character, "Character");

        //TODO Сейчас дергается бот на клиенте при спавне! Он летит из (0;0). Надо либо вернуть обратно эту функцию на просто установку Character.Position,
        //TODO либо подебажить как в обоих реализациях работает телепорт по среди игры, а не только в момент спауна (для этого на кнопку Тест 1 ищет Character игрока/бота в мире и телепортируем его на фиксированную позицию) 
        //TODO Подсказка: игрок имеет имя Character-1-3, а бот Character-1-4
        
        //TODO В доку: для избежания появления юнита на кадр в корах 0;0 и для следов (интерполяции) при телепорте, мы в Ready отключаем интерполяцию на 1 кадр, а Position синхроним через MpSync при спавне
        //TODO Но это +1 нода, так что возможно стоит спавнить юнитов, передавая коры при спавне, через RPC (но тогда мы не увидим других игроков при подключении??!)
        //TODO или через MpSpawner.SpawnFunction + MpSpawner.Spawn в byte[] через сериализатор (протестить как работает синк при подключении игрока по ходу игры), код тут https://gemini.google.com/app/21c0306cc9a7fccb
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.MovementSpeed, 200));
        character.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateAdditive(CharacterStat.RotationSpeed, 360));
        return character;
    }
}
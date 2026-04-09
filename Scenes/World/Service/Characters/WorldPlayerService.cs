using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.Entity.Characters;
using NeonWarfare.Scenes.Entity.Characters.Controller.Player;
using NeonWarfare.Scenes.Entity.Characters.Controller.Remote;
using NeonWarfare.Scenes.Entity.Characters.Stats;

namespace NeonWarfare.Scenes.World.Service.Characters;

public partial class WorldPlayerService : WorldCharacterService
{
    
    public void SpawnPlayer(int peerId)
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
}
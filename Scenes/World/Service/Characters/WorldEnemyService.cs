using NeonWarfare.Scenes.Entity.Character;
using NeonWarfare.Scenes.Entity.Character.Controller.Ai;
using NeonWarfare.Scenes.Entity.Character.Controller.Ai.Impl;

namespace NeonWarfare.Scenes.World.Service.Characters;

public partial class WorldEnemyService : WorldCharacterService
{
    public Character AddBotCharacter(float x, float y, IAiControllerLogic aiLogic = null)
    {
        Character bot = AddCharacter(x, y);
        bot.Controller.SetController(new AiController(aiLogic ?? new AiBattleControllerLogic()));
        return bot;
    }
}
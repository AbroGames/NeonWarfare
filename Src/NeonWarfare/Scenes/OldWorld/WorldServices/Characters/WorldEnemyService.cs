using System;
using Godot;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.Entities.Characters;
using NeonWarfare.Scenes.Entities.Characters.Controller.Ai;
using NeonWarfare.Scenes.Entities.Characters.Controller.Ai.Impl;
using NeonWarfare.Scenes.Entities.Characters.Stats;

namespace NeonWarfare.Scenes.OldWorld.WorldServices.Characters;

public partial class WorldEnemyService : WorldCharacterService
{
    private const double MovementSpeedSpread = 0.2;
    
    /// <summary>Neon swatches an enemy is tinted with at spawn.</summary>
    private static readonly Color[] ColorPool =
    {
        new("ff3b3b"), // red
        new("ff8c00"), // orange
        new("ffe500"), // yellow
        new("3bff6b"), // green
        new("ff00e5"), // magenta
    };
    
    public Character AddBotCharacter(float x, float y, IAiControllerLogic aiLogic = null)
    {
        Character bot = AddCharacter(x, y);
        
        // The roll happens only here on the server; clients receive the resulting stat totals through the stat sync.
        double speedFactor = 1 - MovementSpeedSpread + Random.Shared.NextDouble() * MovementSpeedSpread * 2;
        bot.Stats.AddStatModifier(StatModifier<CharacterStat>.CreateMultiplicative(CharacterStat.MovementSpeed, speedFactor));
        
        // Same as the speed roll: a one-shot RPC at spawn, so clients that join later see the default color.
        bot.Synchronizer.Appearance_OnColorChanged(ColorPool[Random.Shared.Next(ColorPool.Length)]);
        
        bot.Controller.SetController(new AiController(aiLogic ?? new AiBattleControllerLogic()));
        return bot;
    }
}
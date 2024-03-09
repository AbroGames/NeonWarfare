using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class PlayerInputService
{
    
    [EventListener]
    public void OnPlayerInputEvent(PlayerInputEvent playerInputEvent)
    {
        Player player = playerInputEvent.Player;
        InputEvent @event = playerInputEvent.Event;
        
        if (@event.IsActionPressed(Keys.AbilityBasic))
        {
            if (player.BasicAbilityCd.Use())
            {
                EventBus.Publish(new PlayerBasicSkillUseEvent(player));
            }
        }

        if (@event.IsActionPressed(Keys.AbilityAdvanced))
        {
            if (player.AdvancedAbilityCd.Use())
            {
                EventBus.Publish(new PlayerAdvancedSkillUseEvent(player));
            }
        }

        if (@event.IsActionPressed(Keys.WheelUp))
        {
            EventBus.Publish(new PlayerMouseWheelInputEvent(player, WheelEventType.WheelUp));
        }

        if (@event.IsActionPressed(Keys.WheelDown))
        {
            EventBus.Publish(new PlayerMouseWheelInputEvent(player, WheelEventType.WheelDown));
        }
    }
}
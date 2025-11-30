using System;
using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Player;

public class FromPlayerController : IController
{
    
    public void OnPhysicsProcess(double delta, Character character, ControlBlockerHandler controlBlockerHandler)
    {
        throw new NotImplementedException();
    }

    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, ControlBlockerHandler controlBlockerHandler)
    {
        throw new NotImplementedException();
    }
}
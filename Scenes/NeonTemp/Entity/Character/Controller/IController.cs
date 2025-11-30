using System;
using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

public interface IController
{
    public void OnPhysicsProcess(double delta, Character character, ControlBlockerHandler controlBlockerHandler);
    public void OnUnhandledInput(InputEvent @event, Action setAsHandled, Character character, ControlBlockerHandler controlBlockerHandler);
}
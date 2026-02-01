using System;
using Godot;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;

/// <summary>
/// Этот класс не имеет клиентской версии <c>CharacterControllerClient</c>.<br/>
/// Т.к. источником данных может выступать как <c>CharacterController</c> на стороне сервера, так и на стороне клиента.<br/>
/// Поэтому части методов необходимо иметь параметр <c>syncToClient</c>, чтобы избежать циклической синхронизации.
/// </summary>
public class CharacterController
{
    public IController CurrentController { get; private set; }
    private readonly ControlBlockerHandler _controlBlockerHandler = new();
    
    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterController(Character character, CharacterSynchronizer synchronizer, IController controller = null)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
        CurrentController = controller;
    }
    
    public void OnPhysicsProcess(double delta)
    {
        CurrentController.OnPhysicsProcess(delta, _character, _synchronizer, _controlBlockerHandler);
    }

    public void OnUnhandledInput(InputEvent @event, Action setAsHandled)
    {
        CurrentController.OnUnhandledInput(@event, setAsHandled, _character, _synchronizer, _controlBlockerHandler);
    }

    public void OnReceivedMovement(IController.MovementData movementData)
    {
        CurrentController.OnReceivedMovement(_character, _synchronizer, movementData);
    }
    
    public void SetController(IController controller)
    {
        CurrentController = controller;
    }
    
    public void SetControllerToClient(long peerId, IController controller)
    {
        _synchronizer.Controller_OnChange(peerId, controller);
    }
    
    public void AddBlock(ControlBlocker controlBlocker, bool syncToClient = true)
    {
        _controlBlockerHandler.AddBlock(controlBlocker);
        if (syncToClient) _synchronizer.Controller_AddBlock(controlBlocker);
    }

    public void RemoveBlock(ControlBlocker controlBlocker, bool syncToClient = true)
    {
        _controlBlockerHandler.RemoveBlock(controlBlocker);
        if (syncToClient) _synchronizer.Controller_RemoveBlock(controlBlocker);
    }

    public void Teleport(Vector2 position, bool syncToClient = true)
    {
        _character.Position = position;
        if (syncToClient) _synchronizer.Controller_Teleport(position);
    }
}
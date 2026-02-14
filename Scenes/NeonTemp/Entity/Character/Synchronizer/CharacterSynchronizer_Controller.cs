using System;
using Godot;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using static Godot.MultiplayerPeer.TransferModeEnum;
using static Godot.MultiplayerApi.RpcMode;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer
{

    private CharacterController _controller;
    
    private void Controller_InitPostReady(Character character)
    {
        _controller = character.Controller;
    }

    public void Controller_OnChange(long peerId, IController controller)
    {
        int typeId = Services.TypesStorage.GetId(controller.GetType());
        RpcId(peerId, MethodName.Controller_OnChangeRpc, typeId);
    }
    [Rpc(CallLocal = true)]
    private void Controller_OnChangeRpc(int typeId)
    {
        Type targetType = Services.TypesStorage.GetType(typeId);
        IController controller = Activator.CreateInstance(targetType) as IController;
        _controller.SetController(controller);
    }

    public void Controller_AddBlock(ControlBlocker controlBlocker)
    {
        Rpc(MethodName.Controller_AddBlockRpc, MessagePackSerializer.Serialize(controlBlocker));
    }
    [Rpc(CallLocal = false)]
    private void Controller_AddBlockRpc(byte[] controlBlockerBytes)
    {
        var controlBlock = MessagePackSerializer.Deserialize<ControlBlocker>(controlBlockerBytes);
        _controller.AddBlock(controlBlock, false);
    }
    
    public void Controller_RemoveBlock(ControlBlocker controlBlocker)
    {
        Rpc(MethodName.Controller_RemoveBlockRpc, MessagePackSerializer.Serialize(controlBlocker));
    }
    [Rpc(CallLocal = false)]
    private void Controller_RemoveBlockRpc(byte[] controlBlockerBytes)
    {
        var controlBlock = MessagePackSerializer.Deserialize<ControlBlocker>(controlBlockerBytes);
        _controller.RemoveBlock(controlBlock, false);
    }
    
    public void Controller_Teleport(Vector2 position) => 
        Rpc(MethodName.Controller_TeleportRpc, position);
    [Rpc(CallLocal = false)]
    private void Controller_TeleportRpc(Vector2 position)
    {
        _controller.Teleport(position, false);
    }
    
    public void Controller_AddImpulse(Vector2 impulse) => 
        Rpc(MethodName.Controller_AddImpulseRpc, impulse);
    [Rpc(CallLocal = false)]
    private void Controller_AddImpulseRpc(Vector2 impulse)
    {
        _controller.AddImpulse(impulse, false);
    }

    public void Controller_SendMovement(IController.MovementData movementData) => 
        Rpc(MethodName.Controller_SendMovementRpc, MessagePackSerializer.Serialize(movementData));
    [Rpc(AnyPeer, CallLocal = false, TransferMode = Unreliable)]
    private void Controller_SendMovementRpc(byte[] movementDataBytes)
    {
        IController.MovementData movementData = MessagePackSerializer.Deserialize<IController.MovementData>(movementDataBytes);
        _controller.OnReceivedMovement(movementData);
    }
}
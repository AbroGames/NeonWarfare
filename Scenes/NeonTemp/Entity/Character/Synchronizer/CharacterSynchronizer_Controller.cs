using System;
using Godot;
using Godot.Collections;
using KludgeBox.Godot.Nodes.MpSync;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Service;
using static Godot.SceneReplicationConfig.ReplicationMode;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer
{

    private CharacterController _controller;
    
    private void Controller_OnReady()
    {
        _controller = _character.Controller;
    }

    public void Controller_OnChange(long peerId, IController controller)
    {
        int typeId = TypesStorageService.Instance.GetId(controller.GetType());
        RpcId(peerId, MethodName.Controller_OnChangeRpc, typeId);
    }
    [Rpc(CallLocal = false)]
    private void Controller_OnChangeRpc(int typeId)
    {
        Type targetType = TypesStorageService.Instance.GetType(typeId);
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
}
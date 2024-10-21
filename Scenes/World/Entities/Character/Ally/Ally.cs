using System.Diagnostics;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using KludgeBox.Scheduling;
using NeonWarfare.Net;
using NeonWarfare.NetOld.Server;
using NeonWarfare.Utils;
using NeonWarfare.Utils.Cooldown;

namespace NeonWarfare;

public partial class Ally : Character
{
    private const double InertiaCooldown = 0.1;

    private double _movementSpeed;
    private double _movementDir;
    private SingleCooldown _inertiaCooldown = new(InertiaCooldown, false, false);

    public override void _Ready()
    {
        base._Ready();
        _inertiaCooldown.ActionsWhenReady += () => { _movementSpeed = 0; Log.Warning("COOLDOWN WORKED");};
    }

    [EventListener(ListenerSide.Client)]
    public void OnServerMovementEntityPacket(ServerMovementEntityPacket serverMovementEntityPacket)
    {
        Position = Vec(serverMovementEntityPacket.X, serverMovementEntityPacket.Y);
        Rotation = serverMovementEntityPacket.Dir;

        _movementSpeed = serverMovementEntityPacket.MovementSpeed;
        _movementDir = serverMovementEntityPacket.MovementDir;

        if (serverMovementEntityPacket.MovementSpeed != 0)
        {
            //Log.Warning("SET SPEED = " + serverMovementEntityPacket.MovementSpeed + "; DIR = " + serverMovementEntityPacket.MovementDir);
            _inertiaCooldown.Restart(); 
        }
        else
        {
            //Log.Warning("SET SPEED = 0");
            _inertiaCooldown.Reset();
        }
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //If is client
        {
            _inertiaCooldown.Update(delta);
            Position += (Vector2.FromAngle(_movementDir) * _movementSpeed * delta);
            //Log.Warning("MOVE SPEED = " + (Vector2.FromAngle(_movementDir) * _movementSpeed * delta).Length() + "; DIR = " + (Vector2.FromAngle(_movementDir) * _movementSpeed * delta).Angle());
        }
    }
}
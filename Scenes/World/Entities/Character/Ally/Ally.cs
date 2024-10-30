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
    private ManualCooldown _inertiaCooldown = new(InertiaCooldown, false, false);

    public override void _Ready()
    {
        base._Ready();
        _inertiaCooldown.ActionWhenReady += () => { _movementSpeed = 0; };
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //If is client
        {
            _inertiaCooldown.Update(delta);
            Position += Vector2.FromAngle(_movementDir) * _movementSpeed * delta;
        }
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
            _inertiaCooldown.Restart();
        }
        else
        {
            _inertiaCooldown.Reset();
        }
    }
}
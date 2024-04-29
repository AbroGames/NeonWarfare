using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class PlayerService
{
    [EventListener(ListenerSide.Client)]
    public void OnPlayerReady(PlayerReadyEvent e)
    {
        var player = e.Player;
        
        player.Camera = player.GetParent().GetChild<Camera>();

        player.Sprite.Modulate = Root.Instance.PlayerInfo.PlayerColor;
        
        player.SecondaryCd.Ready += () =>
        {
            if (!Input.IsActionPressed(Keys.AttackSecondary)) return;
            Network.SendPacketToServer(new ClientPlayerSecondaryAttackPacket(player.Position.X, player.Position.Y, player.Rotation));
        };
        player.PrimaryCd.Ready += () =>
        {
            if (!Input.IsActionPressed(Keys.AttackPrimary)) return;
            Network.SendPacketToServer(new ClientPlayerPrimaryAttackPacket(player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO костыль для теста снаряда локально. Закомментить передачу по сети, раскомментить строку ниже.
            //TODO new PlayerAttackService().OnServerPlayerPrimaryAttackPacket(new ServerPlayerPrimaryAttackPacket(new Random().NextInt64(), player.Position.X, player.Position.Y, player.Rotation, 2000));
        };
    }

    [EventListener(ListenerSide.Client)]
    public void OnPlayerProcess(PlayerProcessEvent e)
    {
        var (player, delta) = e;
        
        player.Hp += player.RegenHpSpeed * delta;
        player.Hp = Mathf.Min(player.Hp, player.MaxHp);

        player.PrimaryCd.Update(delta);
        player.SecondaryCd.Update(delta);
        player.BasicAbilityCd.Update(delta);
        player.AdvancedAbilityCd.Update(delta);

        player.ShieldSprite.Modulate = player.Modulate with { A = (float)player.HitFlash };
		
        // Camera shift processing
        if (Input.IsActionPressed(Keys.CameraShift))
        {
            var maxShift = player.GetGlobalMousePosition() - player.GlobalPosition;
            var zoomFactor = (player.Camera.Zoom.X + player.Camera.Zoom.Y) / 2;
            player.Camera.PositionShift = maxShift * 0.7 * zoomFactor;
        }
        else
        {
            player.Camera.PositionShift = Vec();
        }
    }
}
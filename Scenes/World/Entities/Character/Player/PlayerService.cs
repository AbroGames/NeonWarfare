using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector.World;

[GameService]
public class PlayerService
{
    [EventListener]
    public void OnPlayerReady(PlayerReadyEvent e)
    {
        var player = e.Player;
        
        player.Camera = player.GetParent().GetChild<Camera>();
        
        player.SecondaryCd.Ready += () =>
        {
            EventBus.Publish(new PlayerAttackSecondaryEvent(player));
        };
    }

    [EventListener]
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

        if (Input.IsActionPressed(Keys.AttackPrimary))
        {
            UpdatePrimaryAttack(player, delta);
        }
    }

    internal void UpdatePrimaryAttack(Player player, double delta)
    {
        if (player.PrimaryCd.Use())
        {
            EventBus.Publish(new PlayerAttackPrimaryEvent(player));
        }
    }
    
}
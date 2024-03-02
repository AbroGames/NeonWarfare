using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

[GameService]
public class PlayerBasicSkillService
{

    [EventListener]
    public void OnPlayerBasicSkillUseEvent(PlayerBasicSkillUseEvent playerBasicSkillUseEvent)
    {
        var player = playerBasicSkillUseEvent.Player;
        var node = Root.Instance.PackedScenes.World.Beam.Instantiate();
        var beam = node as Beam;
        var shaker = player.Camera.ShakeManually();
        beam.Shaker = shaker;
        
        beam.Rotation = player.Rotation - Mathf.Pi / 2;
        player.GetParent().AddChild(beam);
        beam.Position = player.Position;
        beam.Source = player;
        //beam.Modulate = player.Sprite.Modulate;
        beam.Dps *= player.UniversalDamageMultiplier;
        player.Position -= player.Up() * 250;
        
        Audio2D.PlaySoundOn(Sfx.HornImpact3, player);
        Audio2D.PlaySoundOn(Sfx.DeepImpact, player);
    }
}
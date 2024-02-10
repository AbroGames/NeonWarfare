using Game.Content;
using Godot;

namespace Scenes.World.Entities.Character.Player;

public class PlayerBasicSkillService
{
    public PlayerBasicSkillService()
    {
        Root.Instance.EventBus.Subscribe<PlayerBasicSkillUseEvent>(UseSkill);
    }

    public void UseSkill(PlayerBasicSkillUseEvent useEvent)
    {
        var player = useEvent.Player;
        var beam = Root.Instance.PackedScenes.World.Beam.Instantiate() as Beam;
        beam.Rotation = -Mathf.Pi / 2;
        player.AddChild(beam);
        beam.Source = player;
        beam.Modulate = player.Sprite.Modulate;
        beam.Dps *= player.UniversalDamageMultiplier;
        
        Audio2D.PlaySoundOn(Sfx.LaserBeam, player);
        Audio2D.PlaySoundOn(Sfx.LaserBig, player);
        Audio2D.PlaySoundOn(Sfx.Beam, player);
    }
}
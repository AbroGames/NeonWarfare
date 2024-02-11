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
        var node = Root.Instance.PackedScenes.World.Beam.Instantiate();
        var beam = node as Beam;
        beam.Rotation = player.Rotation - Mathf.Pi / 2;
        player.GetParent().AddChild(beam);
        beam.Position = player.Position;
        beam.Source = player;
        //beam.Modulate = player.Sprite.Modulate;
        beam.Dps *= player.UniversalDamageMultiplier;
        
        Audio2D.PlaySoundOn(Sfx.HornImpact3, player);
        Audio2D.PlaySoundOn(Sfx.DeepImpact, player);
    }
}
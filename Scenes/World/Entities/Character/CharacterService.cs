using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class CharacterService
{
    [EventListener]
    public void OnCharacterProcess(CharacterProcessEvent e)
    {
        var (chara, delta) = e;
        
        // flash effect on hit processing
        chara.HitFlash -= 100 * delta;
        chara.HitFlash = Mathf.Max(chara.HitFlash, 0);
        var shader = chara.Sprite.Material as ShaderMaterial;
        shader.SetShaderParameter("colorMaskFactor", chara.HitFlash);
        chara.PrimaryCd.Update(delta);
    }


    [EventListener]
    public void OnCharacterApplyDamage(CharacterApplyDamageRequest request)
    {
        var (character, damage) = request;

        // TryPublish returns true if event was cancelled
        if (EventBus.TryPublish(new CharacterTakeDamageEvent(character, damage))) return;
        
		TakeDamage(character, damage);
    }
    
    public void TakeDamage(Character character, Damage damage)
    	{
    		if (character.Hp <= 0) return;
    		
		    character.HitFlash = 1;
    		var appliedDamage = Mathf.Min(character.Hp, damage.Amount);
		    character.Hp -= damage.Amount;
    		
    		if (character.Hp <= 0)
    		{
			    character.Die();
    			
    			var deathDummy = character.DropDummy();
    			var derbisDummy = character.DropDummy();
    			var death = Fx.CreateDeathFx();
    			var derbis = Fx.CreateDebrisFx();
    			derbis.Modulate = character.Sprite.Modulate;
    			deathDummy.AddChild(death);
    			derbisDummy.AddChild(derbis);
    			//GetParent().MoveChild(derbisDummy, GetIndex() - 1);
    			derbisDummy.ToBackground();
    			
    			Audio2D.PlaySoundAt(Sfx.FuturisticCrack, character.GlobalPosition).PitchVariation(0.25f);
			    character.QueueFree();
    		}
    		
    		var dmgLabel = FloatingLabel.Create();
    		
    		if(appliedDamage <= 0)
    			Log.Debug(appliedDamage.ToString("N0"));
    			
    		dmgLabel.Configure(appliedDamage.ToString("N0"), damage.LabelColor, Mathf.Max(Math.Log(appliedDamage, 20), 0.8));
    		dmgLabel.Position = character.Position + Rand.InsideUnitCircle * 50;
		    character.GetParent().AddChild(dmgLabel);
    	}
}
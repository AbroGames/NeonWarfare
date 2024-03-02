using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector.World;

[GameService]
public class BeamService
{
    [EventListener]
    public void OnBeamSpawned(BeamSpawnedEvent evt)
    {
        var beam = evt.Beam;
        
        beam.StartTtl = beam.Ttl;
        beam.OuterStartWidth = beam.OuterBeamSprite.Scale.Y;
        beam.InnerStartWidth = beam.InnerBeamSprite.Scale.Y;

        beam.DamageCd.Ready += () =>
        {
            DoDamage(beam, beam.DamageCd.Duration);
        };
    }

    [EventListener]
    public void OnBeamPhysicsProcess(BeamPhysicsProcessEvent evt)
    {
        var (beam, delta) = evt;
        
        if (beam.Ttl <= 0)
        {
            beam.Shaker.IsAlive = false;
			
            var dummy = beam.Particles.Drop();
            beam.Particles.Emitting = false;
            dummy.Destruct(beam.Particles.Lifetime * 3);
			
            beam.QueueFree();
        }
		
        var ttlFactor = beam.Ttl / beam.StartTtl;
        var sizeFactor = beam.SizeCurve.Sample(ttlFactor);
		
        beam.Ttl -= delta;
        beam.Ang += 1800 * delta;
        beam.Ang %= 360;

        beam.InnerSpawnSprite.Rotation += Mathf.DegToRad(360 * delta);
        beam.OuterSpawnSprite.Rotation -= Mathf.DegToRad(360 * delta);
        beam.OuterBeamSprite.Scale = beam.OuterBeamSprite.Scale with { Y = beam.OuterStartWidth * sizeFactor + beam.OuterStartWidth * Mathf.Sin(Mathf.DegToRad(beam.Ang)) * 0.07 };
        beam.InnerBeamSprite.Scale = beam.InnerBeamSprite.Scale with { Y = beam.InnerStartWidth * sizeFactor + beam.InnerStartWidth * Mathf.Sin(Mathf.DegToRad(beam.Ang)) * 0.07 };

        beam.DamageCd.Update(delta);
    }
    
    internal void DoDamage(Beam beam, double delta)
    {
        beam.Shaker.Strength = 10 * Mathf.Max(0, 1 - beam.Source.DistanceTo(beam) / beam.ShakeDist); 
		
        var outerDamage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), beam.Dps * delta * 0.5 * beam.Source.UniversalDamageMultiplier, beam.Source);
        var innerDamage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), beam.Dps * delta * 2 * beam.Source.UniversalDamageMultiplier, beam.Source);
		
        var outerOthers = beam.OuterHitArea.GetOverlappingAreas();
        var innerOthers = beam.InnerHitArea.GetOverlappingAreas();
		
        foreach (var area in outerOthers)
        {
            if(area.GetParent() is not Enemy body) continue;
            var distFactor = Mathf.Max(0, 1 - (body.Position - beam.Source.Position).Length() / 2000);
            body.Position += beam.Right() * distFactor * beam.PushVel * beam.Source.UniversalDamageMultiplier * 0.5 * delta;
            body.TakeDamage(outerDamage);
        }
		
        foreach (var area in innerOthers)
        {
            if(area.GetParent() is not Enemy body) continue;
            var distFactor = Mathf.Max(0, 1 - (body.Position - beam.Source.Position).Length() / 2000);
            body.Position += beam.Right() * distFactor * beam.PushVel * beam.Source.UniversalDamageMultiplier * delta;
            body.TakeDamage(innerDamage);
        }
    }
}
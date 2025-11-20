using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Effects.CpuParticles;
using NeonWarfare.Scenes.World.Effects.FloatingLabel;
using NeonWarfare.Scenes.World.Effects.GpuParticles;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;

namespace NeonWarfare.Scripts.Content;

public class Fx
{
    public static CpuParticlesFx CreateDeathFx() => ClientRoot.Instance.PackedScenes.DeathFx.Instantiate<CpuParticlesFx>();
    public static CpuParticlesFx CreateSpawnFx() => ClientRoot.Instance.PackedScenes.SpawnFx.Instantiate<CpuParticlesFx>();
    public static GpuParticlesFx CreateDebrisFx() => ClientRoot.Instance.PackedScenes.DebrisFx.Instantiate<GpuParticlesFx>();
    public static GpuParticlesFx CreateBulletHitFx() => ClientRoot.Instance.PackedScenes.BulletHitFx.Instantiate<GpuParticlesFx>();
    public static AnimatedSprite2D CreateSpriteBulletHitFx() => ClientRoot.Instance.PackedScenes.SpriteBulletHitFx.Instantiate<AnimatedSprite2D>();

    public static (Line2D, Tween) CreateBulletBeamFx(Vector2 fromTo, float width, Color color, double duration)
    {
        var bullet = new Line2D();
        bullet.Points = [fromTo, Vector2.Zero];
        bullet.Width = width;
        bullet.DefaultColor = color;
        
        var tween = bullet.CreateTween();
        tween.TweenProperty(bullet, "width", 0f, duration);
        
        return (bullet, tween);
    }
    
    public static FloatingLabel CreateFloatingLabel() => ClientRoot.Instance.PackedScenes.FloatingLabel.Instantiate<FloatingLabel>();
    public static FloatingLabel CreateFloatingLabel(string text, Color color, float scale)
    {
        FloatingLabel floatingLabel = ClientRoot.Instance.PackedScenes.FloatingLabel.Instantiate<FloatingLabel>();
        floatingLabel.Configure(text, color, scale);
        return floatingLabel;
    }

    public static CpuParticlesFx CreateHealFx()
    {
        var fx = ClientRoot.Instance.PackedScenes.HealFx.Instantiate<CpuParticlesFx>();
        fx.Started += () => PlaySoundFor(Sfx.Heal, fx);
        return fx;
    }
    
    public static CpuParticlesFx CreateResurrectFx()
    {
        var fx = ClientRoot.Instance.PackedScenes.ResurrectionFx.Instantiate<CpuParticlesFx>();
        fx.Started += () => PlaySoundFor(Sfx.Resurrect, fx);
        return fx;
    }

    private static void PlaySoundFor(string soundPath, Node2D target)
    {
        var sfx = Audio2D.PlaySoundAt(soundPath, target.Position, 1f, target.GetParent());
    }
}

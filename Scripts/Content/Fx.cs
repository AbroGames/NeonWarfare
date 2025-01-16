using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Effects.CpuParticles;
using NeonWarfare.Scenes.World.Effects.FloatingLabel;
using NeonWarfare.Scenes.World.Effects.GpuParticles;

namespace NeonWarfare.Scripts.Content;

public class Fx
{
    public static CpuParticlesFx CreateDeathFx() => ClientRoot.Instance.PackedScenes.DeathFx.Instantiate<CpuParticlesFx>();
    public static CpuParticlesFx CreateSpawnFx() => ClientRoot.Instance.PackedScenes.SpawnFx.Instantiate<CpuParticlesFx>();
    public static GpuParticlesFx CreateDebrisFx() => ClientRoot.Instance.PackedScenes.DebrisFx.Instantiate<GpuParticlesFx>();
    public static GpuParticlesFx CreateBulletHitFx() => ClientRoot.Instance.PackedScenes.BulletHitFx.Instantiate<GpuParticlesFx>();
    
    public static FloatingLabel CreateFloatingLabel() => ClientRoot.Instance.PackedScenes.FloatingLabel.Instantiate<FloatingLabel>();
    public static FloatingLabel CreateFloatingLabel(string text, Color color, float scale)
    {
        FloatingLabel floatingLabel = ClientRoot.Instance.PackedScenes.FloatingLabel.Instantiate<FloatingLabel>();
        floatingLabel.Configure(text, color, scale);
        return floatingLabel;
    }
}

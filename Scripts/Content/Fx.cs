using NeoVector.World;

namespace NeoVector;

public class Fx
{
    public static CpuParticlesFx CreateDeathFx() => Root.Instance.PackedScenes.Effects.DeathFx.Instantiate() as CpuParticlesFx;
    public static CpuParticlesFx CreateSpawnFx() => Root.Instance.PackedScenes.Effects.SpawnFx.Instantiate() as CpuParticlesFx;
    public static GpuParticlesFx CreateDebrisFx() => Root.Instance.PackedScenes.Effects.DebrisFx.Instantiate() as GpuParticlesFx;
    public static GpuParticlesFx CreateBulletHitFx() => Root.Instance.PackedScenes.Effects.BulletHitFx.Instantiate() as GpuParticlesFx;
}
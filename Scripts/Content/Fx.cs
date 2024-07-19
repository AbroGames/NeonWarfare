namespace NeonWarfare;

public class Fx
{
    public static CpuParticlesFx CreateDeathFx() => Root.Instance.PackedScenes.Effects.DeathFx.Instantiate<CpuParticlesFx>();
    public static CpuParticlesFx CreateSpawnFx() => Root.Instance.PackedScenes.Effects.SpawnFx.Instantiate<CpuParticlesFx>();
    public static GpuParticlesFx CreateDebrisFx() => Root.Instance.PackedScenes.Effects.DebrisFx.Instantiate<GpuParticlesFx>();
    public static GpuParticlesFx CreateBulletHitFx() => Root.Instance.PackedScenes.Effects.BulletHitFx.Instantiate<GpuParticlesFx>();
}
namespace NeonWarfare;

public class Fx
{
    public static CpuParticlesFx CreateDeathFx() => ClientRoot.Instance.PackedScenes.DeathFx.Instantiate<CpuParticlesFx>();
    public static CpuParticlesFx CreateSpawnFx() => ClientRoot.Instance.PackedScenes.SpawnFx.Instantiate<CpuParticlesFx>();
    public static GpuParticlesFx CreateDebrisFx() => ClientRoot.Instance.PackedScenes.DebrisFx.Instantiate<GpuParticlesFx>();
    public static GpuParticlesFx CreateBulletHitFx() => ClientRoot.Instance.PackedScenes.BulletHitFx.Instantiate<GpuParticlesFx>();
}
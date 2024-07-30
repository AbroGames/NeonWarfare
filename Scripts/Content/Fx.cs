namespace NeonWarfare;

public class Fx
{
    public static CpuParticlesFx CreateDeathFx() => ClientRoot.Instance.PackedScenes.Client.Effects.DeathFx.Instantiate<CpuParticlesFx>();
    public static CpuParticlesFx CreateSpawnFx() => ClientRoot.Instance.PackedScenes.Client.Effects.SpawnFx.Instantiate<CpuParticlesFx>();
    public static GpuParticlesFx CreateDebrisFx() => ClientRoot.Instance.PackedScenes.Client.Effects.DebrisFx.Instantiate<GpuParticlesFx>();
    public static GpuParticlesFx CreateBulletHitFx() => ClientRoot.Instance.PackedScenes.Client.Effects.BulletHitFx.Instantiate<GpuParticlesFx>();
}
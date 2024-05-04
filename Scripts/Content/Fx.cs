namespace NeoVector;

public class Fx
{
    public static NeonWarfare.CpuParticlesFx CreateDeathFx() => NeonWarfare.Root.Instance.PackedScenes.Effects.DeathFx.Instantiate() as NeonWarfare.CpuParticlesFx;
    public static NeonWarfare.CpuParticlesFx CreateSpawnFx() => NeonWarfare.Root.Instance.PackedScenes.Effects.SpawnFx.Instantiate() as NeonWarfare.CpuParticlesFx;
    public static NeonWarfare.GpuParticlesFx CreateDebrisFx() => NeonWarfare.Root.Instance.PackedScenes.Effects.DebrisFx.Instantiate() as NeonWarfare.GpuParticlesFx;
    public static NeonWarfare.GpuParticlesFx CreateBulletHitFx() => NeonWarfare.Root.Instance.PackedScenes.Effects.BulletHitFx.Instantiate() as NeonWarfare.GpuParticlesFx;
}
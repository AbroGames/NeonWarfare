namespace AbroDraft.Scripts.Content;

public class Fx
{
    public static Scenes.World.Effects.CpuParticlesFx CreateDeathFx() => Scenes.Root.Root.Instance.PackedScenes.Effects.DeathFx.Instantiate() as Scenes.World.Effects.CpuParticlesFx;
    public static Scenes.World.Effects.CpuParticlesFx CreateSpawnFx() => Scenes.Root.Root.Instance.PackedScenes.Effects.SpawnFx.Instantiate() as Scenes.World.Effects.CpuParticlesFx;
    public static Scenes.World.Effects.GpuParticlesFx CreateDebrisFx() => Scenes.Root.Root.Instance.PackedScenes.Effects.DebrisFx.Instantiate() as Scenes.World.Effects.GpuParticlesFx;
    public static Scenes.World.Effects.GpuParticlesFx CreateBulletHitFx() => Scenes.Root.Root.Instance.PackedScenes.Effects.BulletHitFx.Instantiate() as Scenes.World.Effects.GpuParticlesFx;
}
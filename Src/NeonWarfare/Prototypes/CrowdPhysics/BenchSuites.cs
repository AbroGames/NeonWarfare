using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The named benchmark suites. `sep` = soft separation only, `phys` = physical enemy-enemy collisions
/// on (separation stays on), `block` = the player and the enemies block each other, `hurt` = hurtboxes,
/// `proj` = projectiles, `boom` = scripted explosions, `walldrop` = periodic walls on top of the crowd.
/// Facing is TowardCrowd unless noted. Both variants must read the suites from here.
/// </summary>
public static class BenchSuites
{
    public const string DefaultSuite = "legacy";

    private static readonly int[] Ramp50 = CountsRamp(from: 50, to: 500, step: 50);
    private static readonly int[] Ramp100 = CountsRamp(from: 100, to: 500, step: 100);

    /// <summary>Ramp50 followed by 600 / 800 / 1000: where the interesting ceiling is expected.</summary>
    private static readonly int[] CoreCounts = [.. Ramp50, 600, 800, 1000];

    private static readonly BenchConfig[] Sanity =
    [
        Sep("wander-sep", SteeringMode.Wander, PlayerMotionMode.Static, counts: [0, 50]),
    ];

    /// <summary>The original four configurations, kept as the baseline of the earlier CSV runs.</summary>
    private static readonly BenchConfig[] Legacy =
    [
        Sep("chase+separation", SteeringMode.Chase, PlayerMotionMode.Static, Ramp50),
        Sep("wall+separation", SteeringMode.Wall, PlayerMotionMode.Static, Ramp50),
        Sep("chase+self-collisions", SteeringMode.Chase, PlayerMotionMode.Static, Ramp50) with
        {
            SelfCollisions = true,
        },
        Sep("chase+projectiles+hurtboxes", SteeringMode.Chase, PlayerMotionMode.Static, Ramp50) with
        {
            Hurtboxes = true,
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
        },
    ];

    private static readonly BenchConfig[] Core =
    [
        Sep("chase-static-sep", SteeringMode.Chase, PlayerMotionMode.Static, CoreCounts),
        Sep("chase-static-phys", SteeringMode.Chase, PlayerMotionMode.Static, CoreCounts) with
        {
            SelfCollisions = true,
        },
        Sep("chase-kite-sep", SteeringMode.Chase, PlayerMotionMode.Kite, CoreCounts),
        Sep("chase-kite-phys", SteeringMode.Chase, PlayerMotionMode.Kite, CoreCounts) with
        {
            SelfCollisions = true,
        },
        Sep("wall-sep", SteeringMode.Wall, PlayerMotionMode.Static, CoreCounts),
        Sep("wall-phys", SteeringMode.Wall, PlayerMotionMode.Static, CoreCounts) with
        {
            SelfCollisions = true,
        },
        Sep("wander-sep", SteeringMode.Wander, PlayerMotionMode.Static, CoreCounts),
    ];

    private static readonly BenchConfig[] Blocking =
    [
        Sep("chase-kite-sep-block", SteeringMode.Chase, PlayerMotionMode.Kite, Ramp100) with
        {
            PlayerBlocks = true,
        },
        Sep("chase-kite-phys-block", SteeringMode.Chase, PlayerMotionMode.Kite, Ramp100) with
        {
            SelfCollisions = true,
            PlayerBlocks = true,
        },
        Sep("chase-static-sep-block", SteeringMode.Chase, PlayerMotionMode.Static, Ramp100) with
        {
            PlayerBlocks = true,
        },
        Sep("chase-static-phys-block", SteeringMode.Chase, PlayerMotionMode.Static, Ramp100) with
        {
            SelfCollisions = true,
            PlayerBlocks = true,
        },
    ];

    private static readonly BenchConfig[] Load =
    [
        Sep("wander-sep-hurt", SteeringMode.Wander, PlayerMotionMode.Static, Ramp50) with
        {
            Hurtboxes = true,
        },
        Sep("wander-sep-proj", SteeringMode.Wander, PlayerMotionMode.Static, Ramp50) with
        {
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
            Facing = FacingMode.Spin,
        },
        Sep("wander-sep-proj-hurt", SteeringMode.Wander, PlayerMotionMode.Static, Ramp50) with
        {
            Hurtboxes = true,
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
            Facing = FacingMode.Spin,
        },
        Sep("chase-kite-sep-proj-hurt", SteeringMode.Chase, PlayerMotionMode.Kite, Ramp50) with
        {
            Hurtboxes = true,
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
        },
        Sep("chase-kite-phys-proj-hurt", SteeringMode.Chase, PlayerMotionMode.Kite, Ramp50) with
        {
            SelfCollisions = true,
            Hurtboxes = true,
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
        },
    ];

    private static readonly BenchConfig[] LoadRate =
    [
        Projectiles("wander-sep-hurt-p300", 300),
        Projectiles("wander-sep-hurt-p600", 600),
        Projectiles("wander-sep-hurt-p900", 900),
        Projectiles("wander-sep-hurt-p1200", 1200),
    ];

    private static readonly BenchConfig[] Events =
    [
        Sep("chase-kite-sep-boom", SteeringMode.Chase, PlayerMotionMode.Kite, Ramp100) with
        {
            Explosions = ExplosionMode.AtPlayer,
        },
        Sep("chase-kite-phys-boom", SteeringMode.Chase, PlayerMotionMode.Kite, Ramp100) with
        {
            SelfCollisions = true,
            Explosions = ExplosionMode.AtPlayer,
        },
        Sep("wall-sep-boom", SteeringMode.Wall, PlayerMotionMode.Static, Ramp100) with
        {
            Explosions = ExplosionMode.IntoWall,
        },
        Sep("wall-phys-boom", SteeringMode.Wall, PlayerMotionMode.Static, Ramp100) with
        {
            SelfCollisions = true,
            Explosions = ExplosionMode.IntoWall,
        },
        Sep("chase-static-sep-walldrop", SteeringMode.Chase, PlayerMotionMode.Static, Ramp100) with
        {
            WallDropPeriod = BenchSpecs.WallDropPeriod,
            WallDropLifetime = BenchSpecs.WallDropLifetime,
        },
        Sep("chase-static-phys-walldrop", SteeringMode.Chase, PlayerMotionMode.Static, Ramp100) with
        {
            SelfCollisions = true,
            WallDropPeriod = BenchSpecs.WallDropPeriod,
            WallDropLifetime = BenchSpecs.WallDropLifetime,
        },
    ];

    private static readonly BenchConfig[] Realtime =
    [
        Sep("chase-kite-sep", SteeringMode.Chase, PlayerMotionMode.Kite, [300, 500]),
        Sep("chase-kite-phys", SteeringMode.Chase, PlayerMotionMode.Kite, [300, 500]) with
        {
            SelfCollisions = true,
        },
        Sep("chase-kite-sep-proj-hurt", SteeringMode.Chase, PlayerMotionMode.Kite, [300, 500]) with
        {
            Hurtboxes = true,
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
        },
        Sep("chase-kite-phys-proj-hurt", SteeringMode.Chase, PlayerMotionMode.Kite, [300, 500]) with
        {
            SelfCollisions = true,
            Hurtboxes = true,
            ProjectileRate = BenchSpecs.DefaultProjectileRate,
        },
    ];

    private static readonly Dictionary<string, BenchConfig[]> Suites = new()
    {
        ["sanity"] = Sanity,
        ["legacy"] = Legacy,
        ["core"] = Core,
        ["blocking"] = Blocking,
        ["load"] = Load,
        ["load-rate"] = LoadRate,
        ["events"] = Events,
        ["realtime"] = Realtime,
    };

    public static IReadOnlyCollection<string> Names => Suites.Keys;

    public static bool TryResolve(string name, out BenchConfig[] configs)
    {
        if (Suites.TryGetValue(name, out BenchConfig[] resolved))
        {
            configs = resolved;
            return true;
        }

        configs = null;
        return false;
    }

    private static int[] CountsRamp(int from, int to, int step)
    {
        var counts = new List<int>();
        for (int count = from; count <= to; count += step)
        {
            counts.Add(count);
        }
        return [.. counts];
    }

    private static BenchConfig Sep(
        string name, SteeringMode steering, PlayerMotionMode motion, int[] counts) => new(
        Name: name,
        Steering: steering,
        PlayerMotion: motion,
        SelfCollisions: false,
        PlayerBlocks: false,
        Hurtboxes: false,
        ProjectileRate: 0,
        Facing: FacingMode.TowardCrowd,
        Explosions: ExplosionMode.None,
        WallDropPeriod: 0,
        WallDropLifetime: 0,
        Counts: counts);

    private static BenchConfig Projectiles(string name, int rate) => Sep(
        name, SteeringMode.Wander, PlayerMotionMode.Static, [300]) with
    {
        Hurtboxes = true,
        ProjectileRate = rate,
        Facing = FacingMode.Spin,
    };
}

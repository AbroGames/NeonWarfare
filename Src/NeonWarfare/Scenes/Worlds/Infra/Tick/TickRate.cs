using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Tick;

/// <summary>The one conversion between seconds and server ticks.</summary>
public static class TickRate
{
    /// <summary>Equals <c>physics/common/physics_ticks_per_second</c> of <c>project.godot</c>.</summary>
    public const int TicksPerSecond = 60;

    /// <summary>Rounds to the nearest tick; a negative, NaN or too long time throws.</summary>
    public static int ToTicks(double seconds)
    {
        double ticks = Math.Round(seconds * TicksPerSecond, MidpointRounding.AwayFromZero);
        if (!(ticks >= 0 && ticks <= int.MaxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Not a valid duration in ticks.");
        }

        return (int) ticks;
    }

    public static double ToSeconds(double ticks) => ticks / TicksPerSecond;
}

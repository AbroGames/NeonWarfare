using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Options;

/// <summary>The options that may change in a running world.</summary>
public record AdjustableWorldOptions
{
    /// <summary>The share of damage a player deals to other players. 0 turns friendly fire off.</summary>
    [field: Replicated] public double FriendlyFireMultiplier { get; set; }

    private AdjustableWorldOptions() { }

    public AdjustableWorldOptions(double friendlyFireMultiplier)
    {
        FriendlyFireMultiplier = friendlyFireMultiplier;
    }

    public static AdjustableWorldOptions GetDefault() => new(friendlyFireMultiplier: 1);

    /// <inheritdoc cref="WorldOptions.Sanitized"/>
    public AdjustableWorldOptions Sanitized()
    {
        return FriendlyFireMultiplier >= 0
            ? this
            : new AdjustableWorldOptions(friendlyFireMultiplier: GetDefault().FriendlyFireMultiplier);
    }
}

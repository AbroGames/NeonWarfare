using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// What separates the two prototypes. The harness owns the whole simulation (intents, soft separation,
/// knockback, metrics); a variant only decides how a body physically realizes the velocity the harness
/// prescribes every tick. The harness discovers the single implementation in the assembly, so the
/// harness code stays byte-identical across the two branches.
/// </summary>
public interface IPhysicsVariant
{
    /// <summary>Human-readable name for the HUD and the CSV.</summary>
    string Title { get; }

    /// <summary>File-name-safe name for the CSV.</summary>
    string FileStamp { get; }

    IBenchPlayerBody CreatePlayer();
    IBenchBotBody CreateBot(float radius);

    /// <summary>A variant-only tuning key in manual mode; the harness offers every key it does not handle itself.</summary>
    TuningKeyResult HandleTuningKey(Key key) => TuningKeyResult.Ignored;

    /// <summary>The current tuning for the HUD; empty when the variant has none.</summary>
    string TuningText => string.Empty;
}

public enum TuningKeyResult
{
    Ignored,
    Handled,

    /// <summary>The key changed what CreatePlayer returns, so the harness replaces the player body in place.</summary>
    RecreatePlayer,
}

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
    IBenchBotBody CreateBot();
}

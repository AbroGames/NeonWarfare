using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Options;

/// <summary>
/// The parameters a world is created with, kept in the world for its whole life.
/// The same record is the options file and the model, see Docs/Data-and-saves.md#files.
/// </summary>
public record WorldOptions
{
    [field: Replicated] public FixedWorldOptions Fixed { get; private set; }
    [field: Replicated] public AdjustableWorldOptions Adjustable { get; private set; }

    private WorldOptions() { }

    public WorldOptions(FixedWorldOptions @fixed, AdjustableWorldOptions adjustable)
    {
        Fixed = @fixed;
        Adjustable = adjustable;
    }

    public static WorldOptions GetDefault() => new(FixedWorldOptions.GetDefault(), AdjustableWorldOptions.GetDefault());

    /// <summary>
    /// Every field out of its range is replaced with its default. Valid options are returned as they are.
    /// </summary>
    public WorldOptions Sanitized()
    {
        AdjustableWorldOptions adjustable = Adjustable.Sanitized();
        return ReferenceEquals(adjustable, Adjustable) ? this : new WorldOptions(Fixed, adjustable);
    }
}

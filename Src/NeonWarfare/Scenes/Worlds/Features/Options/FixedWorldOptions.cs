using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Options;

/// <summary>The options that never change after the world is created.</summary>
public record FixedWorldOptions
{
    [field: Replicated] public bool AllBuildingsUnlocked { get; private set; }

    private FixedWorldOptions() { }

    public FixedWorldOptions(bool allBuildingsUnlocked)
    {
        AllBuildingsUnlocked = allBuildingsUnlocked;
    }

    public static FixedWorldOptions GetDefault() => new(allBuildingsUnlocked: false);
}

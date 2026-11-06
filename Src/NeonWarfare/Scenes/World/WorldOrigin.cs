namespace NeonWarfare.Scenes.World;

/// <summary>
/// Where the state of a World comes from
/// </summary>
public abstract record WorldOrigin
{
    public static readonly WorldOrigin New = new NewWorld();

    private WorldOrigin() { }

    /// <summary>A world created from nothing, on the server.</summary>
    public sealed record NewWorld : WorldOrigin;
}

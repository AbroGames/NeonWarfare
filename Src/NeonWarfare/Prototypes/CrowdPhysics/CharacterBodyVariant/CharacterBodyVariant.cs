namespace NeonWarfare.Prototypes.CrowdPhysics.CharacterBodyVariant;

/// <summary>
/// Variant A: the player and the bots are CharacterBody2D driven by MoveAndSlide. This is the only
/// IPhysicsVariant implementation on this branch; the harness discovers it by type.
/// </summary>
public sealed class CharacterBodyVariant : IPhysicsVariant
{
    public string Title => "A - CharacterBody2D";

    public string FileStamp => "characterbody";

    public IBenchPlayerBody CreatePlayer() => new CharacterBodyBenchPlayer();

    public IBenchBotBody CreateBot() => new CharacterBodyBenchBot();
}

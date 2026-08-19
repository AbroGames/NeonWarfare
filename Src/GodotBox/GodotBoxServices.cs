using KludgeBox.Core.Random;
using KludgeBox.DI;
using KludgeBox.Reflection.Access;

namespace GodotBox;

/// <summary>
/// The services of GodotBox itself, separate from the game's <c>Services</c> — the way KludgeBox keeps its
/// own <c>KludgeBoxServices</c>. There is no <c>Global</c> class and no global using: GodotBox and the
/// game are one assembly, so a second global <c>Di</c> would be ambiguous in all of the game's code.
/// </summary>
internal static class GodotBoxServices
{
    public static readonly DependencyInjector Di = new();
    public static readonly RandomService Rand = new();
    public static MembersScanner MembersScanner => Di.MembersScanner;
}

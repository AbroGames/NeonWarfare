using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// Who may refer to whom, over every reference in the compiled code rather than over source text:
/// <c>Net.IsServer()</c> reaches <c>Services</c> through <c>global using static Services.Global</c> without
/// the word <c>Services</c> anywhere in the file, and a lambda or an async method is a separate
/// compiler-generated type that a syntax check would have to attribute by hand.
/// </summary>
[Collection(GameAssembly.Collection)]
public class LayerReferenceTests
{
    private static readonly string[] ServicesTypes =
        ["NeonWarfare.Scripts.Services", "NeonWarfare.Scripts.Services/Global"];

    /// <summary>
    /// The composition root takes the global services and hands them to the world services through their
    /// constructors — the one place in the World namespace that may.
    /// </summary>
    private static readonly string[] CompositionRoots = [WorldLayers.WorldNamespace + ".World"];

    /// <summary>
    /// <c>Di.Process(this)</c> stays static: it holds no game state, and every node of the World calls it.
    /// </summary>
    private static readonly string[] AllowedServicesMembers = ["Di"];

    [Fact]
    public void WorldNamespace_ReachesServicesOnlyFromCompositionRoot()
    {
        FailureReport report = new("World code reaching the global Services past the composition root");
        GameAssembly game = GameAssembly.Instance;

        foreach (TypeDefinition type in game.Types.Where(WorldLayers.InWorldNamespace))
        {
            if (GameAssembly.SelfAndEnclosing(type).Any(owner => CompositionRoots.Contains(owner.FullName)))
            {
                continue;
            }

            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (!ServicesTypes.Contains(site.Type.FullName))
                {
                    continue;
                }

                string? member = site.Via == null ? null : MemberName(site.Via);
                if (member != null && AllowedServicesMembers.Contains(member))
                {
                    continue;
                }

                string what = member == null ? GameAssembly.ShortName(site.Type) : $"Services.{member}";
                report.Add($"{GameAssembly.Describe(site.From)}: refers to {what} — take it through the " +
                           "constructor from the composition root World");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void CompositionRoots_AreNotStale() =>
        CrossCheck.AssertExemptionsExist(
            nameof(CompositionRoots),
            CompositionRoots,
            name => GameAssembly.Instance.FindByName(name) != null,
            "no such type in the game assembly");

    [Fact]
    public void AllowedServicesMembers_AreNotStale() =>
        CrossCheck.AssertExemptionsExist(
            nameof(AllowedServicesMembers),
            AllowedServicesMembers,
            name => ServicesTypes.Select(GameAssembly.Instance.FindByName).OfType<TypeDefinition>()
                .Any(type => type.Fields.Any(field => field.Name == name)
                             || type.Properties.Any(property => property.Name == name)),
            "Services has no such member");

    /// <summary>
    /// "Presentation and Input never reference Simulation", as a whitelist: a type of the Simulation layers
    /// is referred to only from the Simulation group. Whatever is outside it — Presentation, nodes, HUD,
    /// the network layer — runs where the Simulation may not exist at all, on a client.
    /// </summary>
    [Fact]
    public void Simulation_IsReferencedOnlyBySimulationGroup()
    {
        FailureReport report = new("References to the Simulation from outside the Simulation group");
        GameAssembly game = GameAssembly.Instance;

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.LayerOf(type) is { } own && WorldLayers.SimulationGroup.Contains(own))
            {
                continue;
            }

            HashSet<string> reported = [];
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                TypeDefinition? referenced = game.Find(site.Type);
                if (referenced == null
                    || WorldLayers.LayerOf(referenced) is not { } layer
                    || !WorldLayers.SimulationLayers.Contains(layer))
                {
                    continue;
                }

                string where = GameAssembly.Describe(site.From);
                if (reported.Add($"{where}|{referenced.FullName}"))
                {
                    string ownLayer = WorldLayers.LayerOf(type)?.ToString() ?? "a type outside the layers";
                    report.Add($"{where}: {ownLayer} refers to {GameAssembly.ShortName(referenced)} ({layer})");
                }
            }
        }

        report.AssertEmpty();
    }

    private static string MemberName(MemberReference member) =>
        member is MethodReference { Name: var name } && (name.StartsWith("get_") || name.StartsWith("set_"))
            ? name[4..]
            : member.Name;
}

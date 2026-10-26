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
    private const string InfraNamespace = WorldLayers.WorldNamespace + ".Infra";
    private const string FeaturesNamespace = WorldLayers.WorldNamespace + ".Features";
    private const string HudMailbox = WorldLayers.WorldNamespace + ".Infra.Hud.HudMailbox";
    private const string HudMailboxPost = "Post";

    private static readonly string[] ServicesTypes =
        ["NeonWarfare.Scripts.Services", "NeonWarfare.Scripts.Services/Global"];

    /// <summary>
    /// The composition root — <c>World</c> and the builder of its container — takes the global services, hands
    /// them to the world services through their constructors and wires up the layers of the configuration
    /// (the event handlers, the command handlers and the network command whitelist): the one place in the World
    /// namespace that may.
    /// </summary>
    private static readonly string[] CompositionRoots =
    [
        WorldLayers.WorldNamespace + ".World",
        WorldLayers.WorldNamespace + ".WorldServicesBuilder",
    ];

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
            if (IsCompositionRoot(type))
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
    /// "Presentation and Input never reference Simulation", as a whitelist: a type of the Simulation group —
    /// the server network layer included — is referred to only from the group itself. Whatever is outside
    /// it — Presentation, nodes, HUD, the transport — runs where the Simulation may not exist at all, on a client.
    /// </summary>
    [Fact]
    public void SimulationGroup_IsReferencedOnlyByItself()
    {
        FailureReport report = new("References to the Simulation group from outside it");
        GameAssembly game = GameAssembly.Instance;

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.LayerOf(type) is { } own && WorldLayers.SimulationGroup.Contains(own)
                || IsCompositionRoot(type))
            {
                continue;
            }

            HashSet<string> reported = [];
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                TypeDefinition? referenced = game.Find(site.Type);
                if (referenced == null
                    || WorldLayers.LayerOf(referenced) is not { } layer
                    || !WorldLayers.SimulationGroup.Contains(layer))
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

    /// <summary>
    /// Infra is the machinery every feature plugs into through attributes and reflection, so it knows none of them:
    /// the player is a uid there, and each feature looks its own state up by it. Nor does it know the World root:
    /// the root is the composition root that knows everything, Features included, so through it the rule would be
    /// bypassed.
    /// </summary>
    [Fact]
    public void Infra_DoesNotReferenceFeaturesOrWorldRoot()
    {
        FailureReport report = new("Infra referring to Features or the World root");
        GameAssembly game = GameAssembly.Instance;

        List<TypeDefinition> infra = game.Types.Where(type => InNamespace(type, InfraNamespace)).ToList();
        // Without all three a rename would leave the rule nothing to check
        Assert.NotEmpty(infra);
        Assert.Contains(game.Types, type => InNamespace(type, FeaturesNamespace));
        Assert.Contains(game.Types, IsWorldRoot);

        foreach (TypeDefinition type in infra)
        {
            HashSet<string> reported = [];
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (!InNamespace(site.Type, FeaturesNamespace) && !IsWorldRoot(site.Type))
                {
                    continue;
                }

                string where = GameAssembly.Describe(site.From);
                if (reported.Add($"{where}|{site.Type.FullName}"))
                {
                    report.Add($"{where}: refers to {GameAssembly.ShortName(site.Type)}");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The HUD mailbox is cleared by the frame number alone, which holds only while every post comes from an event
    /// handler, before <c>_Process</c>. A node or a widget posting would also make the HUD talk to itself.
    /// </summary>
    [Fact]
    public void HudMailboxPost_IsCalledOnlyFromPresentations()
    {
        FailureReport report = new("HudMailbox.Post called from outside the Presentation");
        GameAssembly game = GameAssembly.Instance;

        TypeDefinition mailbox = game.FindByName(HudMailbox)
                                 ?? throw new InvalidOperationException($"{HudMailbox} is gone");
        // Without it a rename would leave the rule nothing to check
        Assert.Contains(mailbox.Methods, method => method.Name == HudMailboxPost);

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.LayerOf(type) == Layer.Presentation)
            {
                continue;
            }

            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (site.Type.FullName == HudMailbox && site.Via is MethodReference { Name: HudMailboxPost })
                {
                    string ownLayer = WorldLayers.LayerOf(type)?.ToString() ?? "a type outside the layers";
                    report.Add($"{GameAssembly.Describe(site.From)}: {ownLayer} posts to the HUD mailbox");
                }
            }
        }

        report.AssertEmpty();
    }

    private static bool InNamespace(TypeReference type, string ns)
    {
        string own = GameAssembly.Outermost(type).Namespace;
        return own == ns || own.StartsWith(ns + ".", StringComparison.Ordinal);
    }

    private static bool IsWorldRoot(TypeReference type) =>
        GameAssembly.Outermost(type).Namespace == WorldLayers.WorldNamespace;

    private static bool IsCompositionRoot(TypeDefinition type) =>
        GameAssembly.SelfAndEnclosing(type).Any(owner => CompositionRoots.Contains(owner.FullName));

    private static string MemberName(MemberReference member) =>
        member is MethodReference { Name: var name } && (name.StartsWith("get_") || name.StartsWith("set_"))
            ? name[4..]
            : member.Name;
}

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
    private const string HudMailbox = WorldLayers.WorldNamespace + ".Presentations.HudMailbox";
    private const string HudMailboxPost = "Post";
    private const string CommandInbox = WorldLayers.WorldNamespace + ".ServerNetwork.CommandInbox";
    private const string EnqueueFromDedicatedWindow = "EnqueueFromDedicatedWindow";
    private const string ChatSimulationFacade = WorldLayers.WorldNamespace + ".Simulations.ChatSimulationFacade";
    private const string HandleInputFromDedicatedWindow = "HandleInputFromDedicatedWindow";
    private const string DedicatedWindowCommandHandler =
        WorldLayers.WorldNamespace + ".CommandHandlers.IDedicatedWindowCommandHandler`1";

    private static readonly string[] ServicesTypes =
        ["NeonWarfare.Scripts.Services", "NeonWarfare.Scripts.Services/Global"];

    /// <summary>
    /// The composition root — <c>World</c> and the builder of its container — takes the global services, hands
    /// them to the world services through their constructors and wires up the layers of the configuration
    /// (the dedicated window of the outbox, the event handlers, the command handlers and the network command
    /// whitelist): the one place in the World namespace that may.
    /// </summary>
    private static readonly string[] CompositionRoots =
    [
        WorldLayers.WorldNamespace + ".World",
        WorldLayers.WorldNamespace + ".Composition.WorldServicesBuilder",
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

    /// <summary>
    /// A command enqueued from the dedicated window is processed as the server's own, with admin rights: any other
    /// caller — a peer's packet, a node — would get that trust too.
    /// </summary>
    [Fact]
    public void EnqueueFromDedicatedWindow_IsCalledOnlyFromDedicatedWindow()
    {
        FailureReport report = new("CommandInbox.EnqueueFromDedicatedWindow called from outside the dedicated window");
        GameAssembly game = GameAssembly.Instance;

        TypeDefinition inbox = game.FindByName(CommandInbox)
                               ?? throw new InvalidOperationException($"{CommandInbox} is gone");
        // Without it a rename would leave the rule nothing to check
        Assert.Contains(inbox.Methods, method => method.Name == EnqueueFromDedicatedWindow);

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.LayerOf(type) == Layer.DedicatedWindow)
            {
                continue;
            }

            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (site.Type.FullName == CommandInbox
                    && site.Via is MethodReference { Name: EnqueueFromDedicatedWindow })
                {
                    string ownLayer = WorldLayers.LayerOf(type)?.ToString() ?? "a type outside the layers";
                    report.Add($"{GameAssembly.Describe(site.From)}: {ownLayer} enqueues as the dedicated window");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The facade takes the dedicated window's word for it and gives admin rights: a player handler calling it by
    /// mistake would hand those rights to a player, past the inbox rule above.
    /// </summary>
    [Fact]
    public void HandleInputFromDedicatedWindow_IsCalledOnlyByDedicatedWindowHandler()
    {
        FailureReport report = new("ChatSimulationFacade.HandleInputFromDedicatedWindow called from outside the " +
                                   "dedicated-window handler");
        GameAssembly game = GameAssembly.Instance;

        TypeDefinition facade = game.FindByName(ChatSimulationFacade)
                                ?? throw new InvalidOperationException($"{ChatSimulationFacade} is gone");
        // Without it a rename would leave the rule nothing to check
        Assert.Contains(facade.Methods, method => method.Name == HandleInputFromDedicatedWindow);

        foreach (TypeDefinition type in game.Types)
        {
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (site.Type.FullName == ChatSimulationFacade
                    && site.Via is MethodReference { Name: HandleInputFromDedicatedWindow }
                    && !IsDedicatedWindowProcess(site.From))
                {
                    report.Add($"{GameAssembly.Describe(site.From)}: only the Process of an " +
                               "IDedicatedWindowCommandHandler may call it");
                }
            }
        }

        report.AssertEmpty();
    }

    // The method, not the type: one handler may serve both a player and the window, and its player Process must not
    // pass for the window's
    private static bool IsDedicatedWindowProcess(IMemberDefinition from) =>
        from is MethodDefinition { Name: "Process", Parameters.Count: 1 } method
        && method.DeclaringType.Interfaces
            .Select(implementation => implementation.InterfaceType)
            .OfType<GenericInstanceType>()
            .Any(handler => handler.ElementType.FullName == DedicatedWindowCommandHandler
                            && handler.GenericArguments[0].FullName == method.Parameters[0].ParameterType.FullName);

    private static bool IsCompositionRoot(TypeDefinition type) =>
        GameAssembly.SelfAndEnclosing(type).Any(owner => CompositionRoots.Contains(owner.FullName));

    private static string MemberName(MemberReference member) =>
        member is MethodReference { Name: var name } && (name.StartsWith("get_") || name.StartsWith("set_"))
            ? name[4..]
            : member.Name;
}

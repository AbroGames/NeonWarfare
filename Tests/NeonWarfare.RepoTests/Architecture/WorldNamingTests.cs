using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// A World type says what it is in its name, so a search for the suffix finds every one of a kind, and the name
/// agrees with what the composition root and the protocol go by: the base type and the layer attribute. Both
/// directions are checked: a name that promises a kind the type is not misleads as much as a missing suffix.
/// </summary>
[Collection(GameAssembly.Collection)]
public class WorldNamingTests
{
    private static readonly (string Base, string Suffix)[] Messages =
    [
        (WorldLayers.CommandBase, "Command"),
        (WorldLayers.EventBase, "Event"),
        (WorldLayers.NoticeBase, "Notice"),
    ];

    /// <summary>
    /// Only Features: Infra is the machinery features plug into, and its services are named by their role
    /// (<c>EntitySpawner</c>, <c>HudMailbox</c>, <c>EventOutbox</c>). A command handler takes the command name
    /// before <c>Handler</c>, so its suffix is the shorter one.
    /// </summary>
    private static readonly (Layer Layer, string Suffix)[] FeatureLayers =
    [
        (Layer.Simulation, "Simulation"),
        (Layer.SimulationFacade, "SimulationFacade"),
        (Layer.CommandHandler, "Handler"),
        (Layer.Query, "Query"),
        (Layer.Presentation, "Presentation"),
    ];

    /// <summary>A chat command is text inside <c>SendChatMessageCommand</c>, not a network command.</summary>
    private static readonly string[] NotMessages =
    [
        WorldLayers.FeaturesNamespace + ".Chat.ChatCommands.IChatCommand",
        WorldLayers.FeaturesNamespace + ".Chat.ChatCommands.IListedChatCommand",
    ];

    [Fact]
    public void MessageTypes_EndWithBaseName()
    {
        FailureReport report = new("Commands, events and notices not named after their base type");

        foreach ((string baseName, string suffix) in Messages)
        {
            List<TypeDefinition> derived = WorldTypes()
                .Where(type => WorldLayers.DerivesFrom(type, baseName))
                .ToList();
            // Without them a rename would leave the rule nothing to check
            Assert.NotEmpty(derived);
            foreach (TypeDefinition type in derived)
            {
                if (Name(type).EndsWith(suffix, StringComparison.Ordinal)) continue;

                report.Add($"{GameAssembly.Describe(type)}: derives from {suffix}, but its name does not end with it");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TypesNamedAsMessages_DeriveFromBase()
    {
        FailureReport report = new("Types named as a command, an event or a notice that are not one");

        foreach (TypeDefinition type in WorldTypes())
        {
            if (NotMessages.Contains(type.FullName))
            {
                continue;
            }

            foreach ((string baseName, string suffix) in Messages)
            {
                if (Name(type).EndsWith(suffix, StringComparison.Ordinal)
                    && type.FullName != baseName
                    && !WorldLayers.DerivesFrom(type, baseName))
                {
                    report.Add($"{GameAssembly.Describe(type)}: is named *{suffix}, but does not derive from {suffix}");
                }
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void NotMessages_AreNotStale() =>
        CrossCheck.AssertExemptionsExist(
            nameof(NotMessages),
            NotMessages,
            name => GameAssembly.Instance.FindByName(name) != null,
            "no such type in the game assembly");

    [Fact]
    public void FeatureServices_EndWithLayerSuffix()
    {
        FailureReport report = new("Feature services not named after their layer");

        List<(TypeDefinition Type, Layer Layer)> services = [];
        foreach (TypeDefinition type in FeatureTypes())
        {
            if (WorldLayers.DeclaredLayer(type) is { } declared)
            {
                services.Add((type, declared));
            }
        }
        foreach ((Layer layer, string suffix) in FeatureLayers)
        {
            // Without them a rename would leave the rule nothing to check
            Assert.Contains(services, service => service.Layer == layer);
        }

        foreach ((TypeDefinition type, Layer layer) in services)
        {
            if (Suffix(layer) is { } suffix && !Name(type).EndsWith(suffix, StringComparison.Ordinal))
            {
                report.Add($"{GameAssembly.Describe(type)}: is [{layer}], but its name does not end with {suffix}");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void FeatureTypesNamedAsLayers_CarryTheLayer()
    {
        FailureReport report = new("Feature types named after a layer they are not in");

        foreach (TypeDefinition type in FeatureTypes())
        {
            foreach ((Layer layer, string suffix) in FeatureLayers)
            {
                if (Name(type).EndsWith(suffix, StringComparison.Ordinal) && WorldLayers.DeclaredLayer(type) != layer)
                {
                    report.Add($"{GameAssembly.Describe(type)}: is named *{suffix}, but is not marked [{layer}]");
                }
            }
        }

        report.AssertEmpty();
    }

    private static string? Suffix(Layer layer) =>
        FeatureLayers.Where(pair => pair.Layer == layer).Select(pair => pair.Suffix).FirstOrDefault();

    /// <summary>The name without the generic arity: <c>Foo`1</c> is <c>Foo</c>.</summary>
    private static string Name(TypeDefinition type)
    {
        int arity = type.Name.IndexOf('`');
        return arity < 0 ? type.Name : type.Name[..arity];
    }

    private static IEnumerable<TypeDefinition> WorldTypes() =>
        GameAssembly.Instance.Types.Where(type =>
            WorldLayers.InWorldNamespace(type) && !GameAssembly.IsCompilerGenerated(type));

    private static IEnumerable<TypeDefinition> FeatureTypes() =>
        WorldTypes().Where(WorldLayers.InFeaturesNamespace);
}

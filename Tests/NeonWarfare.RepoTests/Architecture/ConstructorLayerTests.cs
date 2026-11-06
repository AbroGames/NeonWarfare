using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The table "layer → which layers it may accept in its constructor" from the network architecture plan.
/// World services get their dependencies only through the constructor — no statics, no lookup by name —
/// so checking constructor parameters catches every way one layer could reach another. Models and the types of
/// <c>WorldDependencies</c> are open to every layer, except those with side effects beyond the world: they are
/// handed to every World, but only the layer that owns the effect may use them. What else the root registers —
/// the other parameters of <c>WorldServicesBuilder.Build</c>, the entity registry — is open or restricted only by
/// name here, so a new <c>Build</c> parameter needs a decision.
/// Leaf simulations never call each other: an operation with all its effects lives in one facade, and a
/// command handler goes through a facade too, or it would run half an operation. Shared reads go to queries.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ConstructorLayerTests
{
    private const string WorldDependencies = WorldLayers.WorldNamespace + ".WorldDependencies";
    private const string WorldLayer = WorldLayers.WorldNamespace + ".WorldLayer";
    private const string Builder = WorldLayers.WorldNamespace + ".WorldServicesBuilder";
    private const string BuildMethod = "Build";

    private static readonly IReadOnlyDictionary<Layer, Layer[]> AllowedParameterLayers =
        new Dictionary<Layer, Layer[]>
        {
            [Layer.Simulation] = [Layer.Query, Layer.ServerNetwork],
            [Layer.SimulationFacade] = [Layer.Simulation, Layer.SimulationFacade, Layer.Query, Layer.ServerNetwork],
            [Layer.CommandHandler] = [Layer.SimulationFacade, Layer.Query],
            [Layer.ServerNetwork] = [Layer.ServerNetwork, Layer.Query],
            [Layer.Query] = [Layer.Query],
            [Layer.ClientNetwork] = [Layer.ClientNetwork, Layer.Query],
            [Layer.Presentation] = [Layer.Presentation, Layer.Query],
        };

    private static readonly IReadOnlyDictionary<string, Layer[]> RestrictedDependencies =
        new Dictionary<string, Layer[]>
        {
            // The transport would let any layer send in the middle of the tick, past EventOutbox
            [WorldLayers.WorldNamespace + ".Infra.ServerNetwork.IClientsConnection"] = [Layer.ServerNetwork],
            // Registering or placing a node past the spawn would take a NetId past the generator or put an entity
            // nobody replicates into the world
            [WorldLayers.WorldNamespace + ".Infra.Entities.EntityRegistry"] = [Layer.Simulation],
            [WorldLayers.WorldNamespace + ".Infra.Entities.WorldRoot"] = [Layer.Simulation],
        };

    // Open to every layer besides the WorldDependencies types: what the root registers itself, or a view of it
    private static readonly string[] OpenTypes = [WorldLayers.WorldNamespace + ".Infra.Entities.IEntityFinder"];

    [Fact]
    public void Constructors_TakeOnlyAllowedLayers()
    {
        FailureReport report = new("World service constructor parameters the layer table does not allow");
        GameAssembly game = GameAssembly.Instance;
        IReadOnlySet<string> dependencies = WorldDependencyTypes(game);
        IReadOnlySet<string> buildParameters = BuildParameterTypes(game).ToHashSet(StringComparer.Ordinal);
        IReadOnlySet<string> open = dependencies.Concat(OpenTypes).ToHashSet(StringComparer.Ordinal);
        // Otherwise a rename would leave the restriction nothing to check
        foreach (string name in RestrictedDependencies.Keys.Concat(OpenTypes)
                     .Where(name => game.FindByName(name) == null))
        {
            report.Add($"{name} is restricted or open but no longer exists");
        }
        foreach (string parameter in buildParameters
                     .Where(name => !RestrictedDependencies.ContainsKey(name) && !OpenTypes.Contains(name)))
        {
            report.Add($"{parameter} is a Build parameter with no decision: list it in "
                       + $"{nameof(RestrictedDependencies)} or {nameof(OpenTypes)}");
        }

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.DeclaredLayer(type) is not { } layer)
            {
                continue;
            }

            IEnumerable<MethodDefinition> constructors =
                type.Methods.Where(method => method.IsConstructor && !method.IsStatic && method.IsPublic);
            foreach (MethodDefinition constructor in constructors)
            {
                foreach (ParameterDefinition parameter in constructor.Parameters)
                {
                    TypeReference parameterType = parameter.ParameterType;
                    if (RestrictedDependencies.TryGetValue(parameterType.FullName, out Layer[]? owners))
                    {
                        if (!owners.Contains(layer))
                        {
                            report.Add($"{GameAssembly.Describe(constructor)}: {layer} takes " +
                                       $"'{parameter.Name}' of {GameAssembly.ShortName(parameterType)}, " +
                                       $"which only {string.Join(", ", owners)} may take");
                        }
                        continue;
                    }
                    if (open.Contains(parameterType.FullName))
                    {
                        continue;
                    }

                    TypeDefinition? local = game.Find(parameterType);
                    if (local != null && WorldLayers.IsModel(local))
                    {
                        continue;
                    }

                    Layer? parameterLayer = local == null ? null : WorldLayers.DeclaredLayer(local);
                    if (parameterLayer is { } taken && AllowedParameterLayers[layer].Contains(taken))
                    {
                        continue;
                    }

                    string what = parameterLayer is { } other ? $"a {other} service" : "neither a layer nor a model";
                    string allowed = AllowedParameterLayers[layer].Length == 0
                        ? "only models and the composition root's dependencies"
                        : string.Join(", ", AllowedParameterLayers[layer])
                          + ", models and the composition root's dependencies";
                    report.Add($"{GameAssembly.Describe(constructor)}: {layer} takes " +
                               $"'{parameter.Name}' of {GameAssembly.ShortName(parameterType)}, {what}; " +
                               $"a {layer} may take {allowed}");
                }
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void LayerTable_CoversEveryLayerAttribute()
    {
        FailureReport report = new("Layer attributes and layers the architecture tests do not cover");

        List<string> declared = WorldLayers.LayerAttributeTypes().Select(type => type.FullName).ToList();
        Assert.NotEmpty(declared);
        CrossCheck.ReportMissing(
            report,
            declared,
            WorldLayers.KnownAttributes.Keys.ToHashSet(StringComparer.Ordinal),
            name => $"{name} is a layer attribute of the game unknown to {nameof(WorldLayers)}");
        CrossCheck.ReportMissing(
            report,
            WorldLayers.KnownAttributes.Keys,
            declared.ToHashSet(StringComparer.Ordinal),
            name => $"{name} is known to {nameof(WorldLayers)} but no longer declared by the game");

        foreach (Layer layer in Enum.GetValues<Layer>().Where(layer => !AllowedParameterLayers.ContainsKey(layer)))
        {
            report.Add($"{layer} has no row in the constructor table");
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The parameter types of the <c>WorldDependencies</c> record — what the composition root hands to every
    /// service. Read from the record, so a new dependency needs no edit here.
    /// </summary>
    private static IReadOnlySet<string> WorldDependencyTypes(GameAssembly game)
    {
        TypeDefinition record = game.FindByName(WorldDependencies)
                                ?? throw new InvalidOperationException($"{WorldDependencies} is gone");

        // The record also has a copy constructor taking itself.
        MethodDefinition primary = record.Methods.Single(method =>
            method.IsConstructor && !method.IsStatic && method.IsPublic
            && method.Parameters.All(parameter => parameter.ParameterType.FullName != WorldDependencies));
        return primary.Parameters.Select(parameter => parameter.ParameterType.FullName)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The parameter types of <c>WorldServicesBuilder.Build</c> other than the layers and <c>WorldDependencies</c>:
    /// what the World node itself hands to the container. Read from the method, as the record is.
    /// </summary>
    private static IEnumerable<string> BuildParameterTypes(GameAssembly game)
    {
        TypeDefinition builder = game.FindByName(Builder)
                                 ?? throw new InvalidOperationException($"{Builder} is gone");
        MethodDefinition build = builder.Methods.Single(method => method.Name == BuildMethod);
        return build.Parameters
            .Select(parameter => parameter.ParameterType.FullName)
            .Where(name => name != WorldLayer && name != WorldDependencies);
    }
}

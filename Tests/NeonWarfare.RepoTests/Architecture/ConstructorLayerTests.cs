using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The table "layer → which layers it may accept in its constructor" from the network architecture plan.
/// World services get their dependencies only through the constructor — no statics, no lookup by name —
/// so checking constructor parameters catches every way one layer could reach another. Models and the
/// types of <c>WorldDependencies</c> are open to every layer, except those with network side effects: they are
/// handed to every World, but only the layer that owns sending may use them.
/// Leaf simulations never call each other: an operation with all its effects lives in one facade, and a
/// command handler goes through a facade too, or it would run half an operation. Shared reads go to queries.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ConstructorLayerTests
{
    private const string WorldDependencies = WorldLayers.WorldNamespace + ".Composition.WorldDependencies";

    private static readonly IReadOnlyDictionary<Layer, Layer[]> AllowedParameterLayers =
        new Dictionary<Layer, Layer[]>
        {
            [Layer.Simulation] = [Layer.Query, Layer.ServerNetwork],
            [Layer.SimulationFacade] = [Layer.Simulation, Layer.SimulationFacade, Layer.Query, Layer.ServerNetwork],
            [Layer.CommandHandler] = [Layer.SimulationFacade, Layer.Query],
            [Layer.ServerNetwork] = [Layer.ServerNetwork],
            [Layer.Query] = [Layer.Query],
            [Layer.ClientNetwork] = [Layer.ClientNetwork],
            [Layer.Presentation] = [Layer.Presentation, Layer.Query],
        };

    // The transport would let any layer send in the middle of the tick, past EventOutbox
    private static readonly IReadOnlyDictionary<string, Layer[]> RestrictedDependencies =
        new Dictionary<string, Layer[]>
        {
            [WorldLayers.WorldNamespace + ".ServerNetwork.IClientsConnection"] = [Layer.ServerNetwork],
        };

    [Fact]
    public void Constructors_TakeOnlyAllowedLayers()
    {
        FailureReport report = new("World service constructor parameters the layer table does not allow");
        GameAssembly game = GameAssembly.Instance;
        IReadOnlySet<string> dependencies = WorldDependencyTypes(game);
        // Otherwise a rename would leave the restriction nothing to check
        foreach (string restricted in RestrictedDependencies.Keys.Where(name => !dependencies.Contains(name)))
        {
            report.Add($"{restricted} is restricted but is no longer a WorldDependencies type");
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
                    if (dependencies.Contains(parameterType.FullName))
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
                        ? "only models and WorldDependencies"
                        : string.Join(", ", AllowedParameterLayers[layer]) + ", models and WorldDependencies";
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
}

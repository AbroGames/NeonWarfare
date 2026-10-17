using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>The layers of the World, as the layer attributes of the game declare them.</summary>
public enum Layer
{
    Simulation,
    SimulationFacade,
    CommandHandler,
    ServerNetwork,
    DedicatedWindow,
    Query,
    ClientNetwork,
    Presentation,
}

/// <summary>
/// What a type of the game assembly is in terms of the World layer rules. The layer of a type is its layer
/// attribute, or the attribute of the nearest enclosing type: lambdas, local functions and async state
/// machines are compiler-generated nested types and belong to the type they are written in.
/// </summary>
public static class WorldLayers
{
    public const string WorldNamespace = "NeonWarfare.Scenes.World";

    private const string CompositionNamespace = WorldNamespace + ".Composition";
    private const string LayerAttributeBase = CompositionNamespace + ".WorldServiceAttribute";
    private const string ReplicatedAttribute = "RepliCAT.ReplicatedAttribute";
    private const string GodotObject = "Godot.GodotObject";
    private const string FacadeArgument = "Facade";

    /// <summary>
    /// Every layer attribute the tests know, by Cecil full name. A new subclass of WorldServiceAttribute
    /// must be added here — until then <see cref="LayerOf"/> throws on any type carrying it.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<CustomAttribute, Layer>> KnownAttributes =
        new Dictionary<string, Func<CustomAttribute, Layer>>(StringComparer.Ordinal)
        {
            [CompositionNamespace + ".SimulationAttribute"] = attribute =>
                attribute.Properties.Any(named => named.Name == FacadeArgument && named.Argument.Value is true)
                    ? Layer.SimulationFacade
                    : Layer.Simulation,
            [CompositionNamespace + ".CommandHandlerAttribute"] = _ => Layer.CommandHandler,
            [CompositionNamespace + ".ServerNetworkAttribute"] = _ => Layer.ServerNetwork,
            [CompositionNamespace + ".DedicatedWindowAttribute"] = _ => Layer.DedicatedWindow,
            [CompositionNamespace + ".QueryAttribute"] = _ => Layer.Query,
            [CompositionNamespace + ".ClientNetworkAttribute"] = _ => Layer.ClientNetwork,
            [CompositionNamespace + ".PresentationAttribute"] = _ => Layer.Presentation,
        };

    /// <summary>
    /// The layers whose code runs only where the Simulation does and may reach it. The dedicated window is here
    /// because it exists only on a dedicated server, which always has the Simulation.
    /// </summary>
    public static readonly IReadOnlySet<Layer> SimulationGroup = new HashSet<Layer>
    {
        Layer.Simulation, Layer.SimulationFacade, Layer.CommandHandler, Layer.ServerNetwork,
        Layer.DedicatedWindow
    };

    /// <summary>The layers allowed to write replicated state, besides the models themselves.</summary>
    public static readonly IReadOnlySet<Layer> SimulationLayers =
        new HashSet<Layer> { Layer.Simulation, Layer.SimulationFacade };

    /// <summary>The layer declared on the type itself, ignoring enclosing types.</summary>
    public static Layer? DeclaredLayer(TypeDefinition type)
    {
        Layer? found = null;
        foreach (CustomAttribute attribute in type.CustomAttributes)
        {
            string name = attribute.AttributeType.FullName;
            if (KnownAttributes.TryGetValue(name, out Func<CustomAttribute, Layer>? layer))
            {
                found = layer(attribute);
            }
            else if (IsLayerAttribute(attribute.AttributeType))
            {
                throw new InvalidOperationException(
                    $"{GameAssembly.ShortName(type)} carries {name}, a layer attribute the architecture tests " +
                    $"do not know. Add it to {nameof(WorldLayers)}.{nameof(KnownAttributes)} and to the " +
                    "constructor table.");
            }
        }

        return found;
    }

    /// <summary>The layer of the type or of the nearest enclosing type that has one.</summary>
    public static Layer? LayerOf(TypeDefinition type) =>
        GameAssembly.SelfAndEnclosing(type).Select(DeclaredLayer).FirstOrDefault(layer => layer != null);

    /// <summary>
    /// A model: a class or struct that is not a Godot object and declares a <c>[Replicated]</c> field or
    /// property — the backing field of <c>[field: Replicated]</c> included.
    /// </summary>
    public static bool IsModel(TypeDefinition type) =>
        (type.IsClass || type.IsValueType)
        && !type.IsInterface
        && (type.Fields.Any(IsReplicated) || type.Properties.Any(IsReplicated))
        && !IsGodotObject(type);

    /// <summary>The model the type is, or is nested in; null for anything else.</summary>
    public static TypeDefinition? ModelOwner(TypeDefinition type) =>
        GameAssembly.SelfAndEnclosing(type).FirstOrDefault(IsModel);

    public static bool IsReplicated(ICustomAttributeProvider member) =>
        member.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == ReplicatedAttribute);

    /// <summary>True when the type is under <see cref="WorldNamespace"/> or one of its sub-namespaces.</summary>
    public static bool InWorldNamespace(TypeReference type)
    {
        string ns = GameAssembly.Outermost(type).Namespace;
        return ns == WorldNamespace || ns.StartsWith(WorldNamespace + ".", StringComparison.Ordinal);
    }

    /// <summary>Every concrete layer attribute declared by the game.</summary>
    public static IEnumerable<TypeDefinition> LayerAttributeTypes() =>
        GameAssembly.Instance.Types.Where(type => !type.IsAbstract && IsLayerAttribute(type));

    private static bool IsLayerAttribute(TypeReference attributeType)
    {
        for (TypeDefinition? current = GameAssembly.Instance.Find(attributeType);
             current != null;
             current = current.BaseType == null ? null : GameAssembly.Instance.Find(current.BaseType))
        {
            if (current.BaseType?.FullName == LayerAttributeBase)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGodotObject(TypeDefinition type)
    {
        for (TypeReference? current = type.BaseType; current != null; current = current.Resolve()?.BaseType)
        {
            if (current.FullName == GodotObject)
            {
                return true;
            }

            // The framework is not in the game output folder, and nothing under it is a Godot object.
            if (GameAssembly.Outermost(current).Namespace.StartsWith("System", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return false;
    }
}

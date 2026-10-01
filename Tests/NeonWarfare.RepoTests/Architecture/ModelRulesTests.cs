using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// Models are the replicated state: plain objects that the server writes, RepliCAT carries and the save
/// stores. They know nothing outside themselves, and only the Simulation changes them — anything else
/// writing a model would either be overwritten by the next tick or, on the server, never reach a client
/// as a deliberate change.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ModelRulesTests
{
    private const string ManualReplication = "RepliCAT.ManualReplication";
    private const string MarkDirty = "MarkDirty";

    /// <summary>
    /// The public methods of the replicated collections that change them, by Cecil element type name.
    /// <see cref="ReplicatedCollectionReaders"/> holds the rest; together they must cover the whole public
    /// API, so a method RepliCAT adds cannot slip past as a silent read.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> ReplicatedCollectionMutators =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["RepliCAT.ReplicatedList`1"] =
                ["set_Item", "Add", "AddRange", "Insert", "RemoveAt", "Remove", "Clear", "Sort"],
            ["RepliCAT.ReplicatedDictionary`2"] = ["set_Item", "Add", "TryAdd", "Remove", "Clear"],
            ["RepliCAT.ReplicatedSet`1"] =
            [
                "Add", "Remove", "RemoveWhere", "Clear",
                "UnionWith", "IntersectWith", "ExceptWith", "SymmetricExceptWith",
            ],
        };

    private static readonly string[] ReplicatedCollectionReaders =
    [
        ".ctor", "get_Count", "get_Keys", "get_Values", "get_Item", "ContainsKey", "ContainsValue", "TryGetValue",
        "GetEnumerator", "IndexOf", "Contains", "CopyTo", "IsSubsetOf", "IsProperSubsetOf", "IsSupersetOf",
        "IsProperSupersetOf", "Overlaps", "SetEquals",
    ];

    /// <summary>
    /// Value types of the engine a model may hold — <c>Color</c>, <c>Vector2</c> and the like — plus the math
    /// helpers working on them. A class of the engine, a node above all, is never model state.
    /// </summary>
    private static readonly string[] AllowedGodotClasses = ["Godot.Mathf"];

    [Fact]
    public void Models_ReferenceOnlyAllowedTypes()
    {
        FailureReport report = new("Models referring to something outside the model");
        GameAssembly game = GameAssembly.Instance;
        List<TypeDefinition> models = game.Types.Where(WorldLayers.IsModel).ToList();
        Assert.NotEmpty(models);

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.ModelOwner(type) is not { } model)
            {
                continue;
            }

            HashSet<string> reported = [];
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (IsAllowedInModel(game, site.Type) || !reported.Add(site.Type.FullName))
                {
                    continue;
                }

                report.Add($"{GameAssembly.Describe(site.From)}: model {GameAssembly.ShortName(model)} refers " +
                           $"to {site.Type.FullName} — a model holds only primitives, engine value types, " +
                           "RepliCAT collections, game enums and other models");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// A write is a store into a <c>[Replicated]</c> field, a call to the setter of a <c>[Replicated]</c>
    /// property, a mutator of a replicated collection, <c>ManualReplication.MarkDirty</c>, or a call to a
    /// model method that does any of these, however deep. Calling a constructor is not a write, and neither
    /// is a constructor initializing a field of its own type. Writes are allowed only in the Simulation layers
    /// and in the models themselves.
    /// </summary>
    [Fact]
    public void ReplicatedState_IsWrittenOnlyBySimulation()
    {
        FailureReport report = new("Replicated state written outside the Simulation");
        GameAssembly game = GameAssembly.Instance;
        HashSet<MethodDefinition> writingModelMethods = WritingModelMethods(game);

        foreach (TypeDefinition type in game.Types)
        {
            bool allowed = WorldLayers.ModelOwner(type) != null
                           || WorldLayers.LayerOf(type) is { } layer && WorldLayers.SimulationLayers.Contains(layer);
            if (allowed)
            {
                continue;
            }

            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
            {
                foreach (string write in Writes(game, method, writingModelMethods).Distinct())
                {
                    string ownLayer = WorldLayers.LayerOf(type)?.ToString() ?? "a type outside the Simulation";
                    report.Add($"{GameAssembly.Describe(method)}: {ownLayer} {write}");
                }
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void ReplicatedCollectionApi_IsClassified()
    {
        FailureReport report = new("Public methods of RepliCAT collections neither a mutator nor a reader");
        ModuleDefinition replicat = ReplicatModule(GameAssembly.Instance);

        foreach ((string name, string[] mutators) in ReplicatedCollectionMutators)
        {
            TypeDefinition collection = replicat.GetType(name)
                                        ?? throw new InvalidOperationException($"RepliCAT has no {name}");
            foreach (MethodDefinition method in collection.Methods.Where(method => method.IsPublic))
            {
                if (!mutators.Contains(method.Name) && !ReplicatedCollectionReaders.Contains(method.Name))
                {
                    report.Add($"{name}::{method.Name} — add it to the mutators or to the readers");
                }
            }
        }

        report.AssertEmpty();
    }

    private static bool IsAllowedInModel(GameAssembly game, TypeReference type)
    {
        string ns = GameAssembly.Outermost(type).Namespace;
        if (ns is "System" or "RepliCAT"
            || ns.StartsWith("System.", StringComparison.Ordinal)
            || ns.StartsWith("RepliCAT.", StringComparison.Ordinal))
        {
            return true;
        }

        if (ns == "Godot")
        {
            return AllowedGodotClasses.Contains(type.FullName) || type.Resolve() is { IsValueType: true };
        }

        if (game.Find(type) is not { } local)
        {
            return false;
        }

        return local.IsEnum
               || WorldLayers.ModelOwner(local) != null
               || GameAssembly.SelfAndEnclosing(local).Any(IsCompilerGenerated);
    }

    private static bool IsCompilerGenerated(TypeDefinition type) =>
        type.Name.StartsWith('<')
        || type.CustomAttributes.Any(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    /// <summary>
    /// The model methods that write replicated state, directly or through other model methods, as a fixed
    /// point. A delegate taken from a writing method counts as a call: <c>players.ForEach(p => p.Nick = …)</c>
    /// writes through the lambda it passes.
    /// </summary>
    private static HashSet<MethodDefinition> WritingModelMethods(GameAssembly game)
    {
        List<MethodDefinition> candidates = game.Types
            .Where(type => WorldLayers.ModelOwner(type) != null)
            .SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .ToList();

        HashSet<MethodDefinition> writing = [];
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (MethodDefinition method in candidates.Where(method => !writing.Contains(method)))
            {
                if (Writes(game, method, writing).Any())
                {
                    writing.Add(method);
                    grew = true;
                }
            }
        }

        return writing;
    }

    /// <summary>What <paramref name="method"/> writes, one phrase per write, for the failure message.</summary>
    private static IEnumerable<string> Writes(
        GameAssembly game,
        MethodDefinition method,
        IReadOnlySet<MethodDefinition> writingModelMethods)
    {
        foreach (Instruction instruction in method.Body.Instructions)
        {
            if (instruction.OpCode.Code is Code.Stfld or Code.Stsfld && instruction.Operand is FieldReference field)
            {
                bool ownInitialization = method.IsConstructor
                                         && field.DeclaringType.FullName == method.DeclaringType.FullName;
                if (!ownInitialization && game.Find(field.DeclaringType) != null && field.Resolve() is { } definition
                    && WorldLayers.IsReplicated(definition))
                {
                    yield return $"writes {GameAssembly.ShortName(field.DeclaringType)}::{field.Name}";
                }

                continue;
            }

            if (instruction.Operand is not MethodReference called
                || instruction.OpCode.Code is not (Code.Call or Code.Callvirt or Code.Ldftn or Code.Ldvirtftn))
            {
                continue;
            }

            string declaring = called.DeclaringType.GetElementType().FullName;
            string target = $"{GameAssembly.ShortName(called.DeclaringType.GetElementType())}::{called.Name}";
            if (ReplicatedCollectionMutators.TryGetValue(declaring, out string[]? mutators)
                && mutators.Contains(called.Name))
            {
                yield return $"changes a replicated collection through {target}";
            }
            else if (declaring == ManualReplication && called.Name == MarkDirty)
            {
                yield return $"marks replicated state dirty through {target}";
            }
            else if (game.Find(called.DeclaringType) != null && called.Resolve() is { IsConstructor: false } resolved)
            {
                if (IsReplicatedSetter(resolved))
                {
                    yield return $"sets the replicated property {target}";
                }
                else if (writingModelMethods.Contains(resolved))
                {
                    yield return $"calls {target}, which writes replicated state";
                }
            }
        }
    }

    private static bool IsReplicatedSetter(MethodDefinition method) =>
        method.IsSetter
        && method.DeclaringType.Properties.Any(property => property.SetMethod == method
                                                           && WorldLayers.IsReplicated(property));

    private static ModuleDefinition ReplicatModule(GameAssembly game)
    {
        AssemblyNameReference reference = game.Module.AssemblyReferences.Single(name => name.Name == "RepliCAT");
        return game.Module.AssemblyResolver.Resolve(reference).MainModule;
    }
}

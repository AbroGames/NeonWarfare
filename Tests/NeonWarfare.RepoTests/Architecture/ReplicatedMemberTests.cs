using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The shape of a <c>[Replicated]</c> member. On receipt RepliCAT simply assigns a field, so logic in a property
/// setter would run on a client only — a property is kept out. A plain collection would be either rejected by
/// RepliCAT at runtime or, inside a type with its own codec, sent as a whole; the replicated ones track their slots.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ReplicatedMemberTests
{
    private static readonly string[] ReplicatedCollections =
        ["RepliCAT.ReplicatedList`1", "RepliCAT.ReplicatedDictionary`2", "RepliCAT.ReplicatedSet`1"];

    private static readonly string[] CollectionNamespaces = ["System.Collections", "Godot.Collections"];

    [Fact]
    public void ReplicatedMembers_AreFields()
    {
        FailureReport report = new("[Replicated] on a property");

        foreach (TypeDefinition type in GameAssembly.Instance.Types)
        {
            foreach (PropertyDefinition property in type.Properties.Where(WorldLayers.IsReplicated))
            {
                report.Add($"{GameAssembly.ShortName(type)}::{property.Name} — use [field: Replicated] on an " +
                           "auto-property or a field");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void ReplicatedFields_UseReplicatedCollections()
    {
        FailureReport report = new("[Replicated] fields holding a plain collection");
        GameAssembly game = GameAssembly.Instance;
        List<FieldDefinition> fields = game.Types.SelectMany(type => type.Fields).Where(WorldLayers.IsReplicated)
            .ToList();
        // Otherwise a rename of the attribute would leave this test nothing to check
        Assert.NotEmpty(fields);

        foreach (FieldDefinition field in fields)
        {
            foreach (TypeReference plain in PlainCollections(game, field.FieldType).Distinct())
            {
                report.Add($"{GameAssembly.ShortName(field.DeclaringType)}::{field.Name}: {plain.FullName} — use " +
                           "ReplicatedList, ReplicatedDictionary or ReplicatedSet");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>The plain collections in <paramref name="type"/> itself and in its generic arguments.</summary>
    private static IEnumerable<TypeReference> PlainCollections(GameAssembly game, TypeReference type)
    {
        if (type is GenericInstanceType generic)
        {
            foreach (TypeReference argument in generic.GenericArguments)
            {
                foreach (TypeReference plain in PlainCollections(game, argument))
                {
                    yield return plain;
                }
            }
        }

        if (IsPlainCollection(game, type))
        {
            yield return type;
        }
    }

    // By name, not Resolve(): the resolver of GameAssembly does not reach the framework reference assemblies
    private static bool IsPlainCollection(GameAssembly game, TypeReference type)
    {
        if (type.IsArray)
        {
            return true;
        }

        string name = type.GetElementType().FullName;
        if (ReplicatedCollections.Contains(name))
        {
            return false;
        }

        if (IsCollectionNamespace(GameAssembly.Outermost(type).Namespace))
        {
            return true;
        }

        // A game type is a collection through a base type or an interface, however deep
        if (game.Find(type) is not { } own)
        {
            return false;
        }

        return own.BaseType is { } baseType && IsPlainCollection(game, baseType)
               || own.Interfaces.Any(implemented => IsPlainCollection(game, implemented.InterfaceType));
    }

    private static bool IsCollectionNamespace(string ns) =>
        CollectionNamespaces.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal));
}

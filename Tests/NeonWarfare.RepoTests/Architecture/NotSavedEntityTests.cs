using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The save keeps a <c>[NotSaved]</c> entity without its state, and a load gets it fresh from its scene. Its
/// replicated members are then whatever the constructor makes of them: a member that is not a readonly field set
/// there could be null after a load, and RepliCAT could not fill it on a client either.
/// </summary>
[Collection(GameAssembly.Collection)]
public class NotSavedEntityTests
{
    private const string NotSavedAttribute = WorldLayers.WorldNamespace + ".Infra.Entities.NotSavedAttribute";
    private const string ReplicatedAttribute = "RepliCAT.ReplicatedAttribute";

    [Fact]
    public void NotSavedEntities_HoldReplicatedStateInReadonlyFieldsSetInTheConstructor()
    {
        FailureReport report = new("Replicated members of [NotSaved] entities that a load could leave null");
        GameAssembly game = GameAssembly.Instance;
        List<TypeDefinition> entities = game.Types
            .Where(type => type.CustomAttributes
                .Any(attribute => attribute.AttributeType.FullName == NotSavedAttribute))
            .ToList();
        // Otherwise a rename of the attribute would leave this test nothing to check
        Assert.NotEmpty(entities);

        // By name: the Godot source generators put a partial of every node first, so a location would point there
        foreach (TypeDefinition type in entities)
        {
            string owner = GameAssembly.ShortName(type);
            foreach (PropertyDefinition property in type.Properties.Where(IsReplicated))
            {
                report.Add($"{owner}::{property.Name}: a replicated property — make it a readonly field");
            }

            List<MethodDefinition> constructors = type.Methods
                .Where(method => method.IsConstructor && !method.IsStatic && !ChainsToOwnConstructor(method, type))
                .ToList();
            foreach (FieldDefinition field in type.Fields.Where(IsReplicated))
            {
                if (!field.IsInitOnly)
                {
                    report.Add($"{owner}::{field.Name}: not readonly");
                }
                if (constructors.Any(constructor => !Stores(constructor, field)))
                {
                    report.Add($"{owner}::{field.Name}: a constructor does not set it — initialize the field");
                }
            }
        }

        report.AssertEmpty();
    }

    private static bool IsReplicated(ICustomAttributeProvider member) =>
        member.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == ReplicatedAttribute);

    // A constructor calling this(...) gets the fields from the one it calls
    private static bool ChainsToOwnConstructor(MethodDefinition constructor, TypeDefinition type) =>
        constructor.Body.Instructions.Any(instruction =>
            instruction.OpCode == OpCodes.Call
            && instruction.Operand is MethodReference { Name: ".ctor" } called
            && called.DeclaringType.FullName == type.FullName);

    private static bool Stores(MethodDefinition constructor, FieldDefinition field) =>
        constructor.Body.Instructions.Any(instruction =>
            instruction.OpCode == OpCodes.Stfld
            && instruction.Operand is FieldReference stored
            && stored.Resolve() == field);
}

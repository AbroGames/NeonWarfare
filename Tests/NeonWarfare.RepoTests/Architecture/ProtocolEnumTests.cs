using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// <c>ProtocolHasher</c> hashes the members of the mapped enums only, and only the game's own types are mapped. An
/// enum of another assembly in a MessagePack key or a <c>[Replicated]</c> member would go into the hash by its name
/// alone, so a build of another engine version could read its values wrong and still pass the handshake.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ProtocolEnumTests
{
    private const string MessagePackKey = "MessagePack.KeyAttribute";

    [Fact]
    public void WireMembers_HoldOnlyGameEnums()
    {
        FailureReport report = new("Enums of another assembly in a MessagePack key or a [Replicated] member");
        GameAssembly game = GameAssembly.Instance;
        List<(IMemberDefinition Member, TypeReference Type)> members = game.Types
            .SelectMany(type => type.Fields.Where(IsOnWire).Select(field => ((IMemberDefinition)field, field.FieldType))
                .Concat(type.Properties.Where(IsOnWire)
                    .Select(property => ((IMemberDefinition)property, property.PropertyType))))
            .ToList();
        // Otherwise a rename of either attribute would leave this test nothing to check
        Assert.NotEmpty(members);

        foreach ((IMemberDefinition member, TypeReference type) in members)
        {
            foreach (TypeReference foreign in ForeignEnums(game, type).Distinct())
            {
                report.Add($"{GameAssembly.Describe(member)}: {foreign.FullName} — wrap it in a game enum");
            }
        }

        report.AssertEmpty();
    }

    private static bool IsOnWire(ICustomAttributeProvider member) =>
        WorldLayers.IsReplicated(member)
        || member.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == MessagePackKey);

    private static IEnumerable<TypeReference> ForeignEnums(GameAssembly game, TypeReference type)
    {
        if (type is GenericInstanceType generic)
        {
            foreach (TypeReference argument in generic.GenericArguments)
            {
                foreach (TypeReference foreign in ForeignEnums(game, argument))
                {
                    yield return foreign;
                }
            }
        }

        if (type is TypeSpecification specification)
        {
            foreach (TypeReference foreign in ForeignEnums(game, specification.ElementType))
            {
                yield return foreign;
            }
        }
        else if (game.Find(type) == null && type.Resolve() is { IsEnum: true })
        {
            yield return type;
        }
    }
}

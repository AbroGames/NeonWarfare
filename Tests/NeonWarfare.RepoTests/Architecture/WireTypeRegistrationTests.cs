using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// <c>ProtocolCodecs</c> is the one list of the custom wire types. MessagePack finds a formatter through its assembly
/// attribute alone, so nothing else would notice a formatter missing from the list.
/// </summary>
[Collection(GameAssembly.Collection)]
public class WireTypeRegistrationTests
{
    private const string GameNamespace = "NeonWarfare.";
    private const string ProtocolCodecs = WorldLayers.WorldNamespace + ".Infra.Protocol.ProtocolCodecs";
    private const string Formatter = "MessagePack.Formatters.IMessagePackFormatter`1";
    private const string ReplicationCodec = "RepliCAT.Codecs.IReplicationCodec`1";

    [Theory]
    [InlineData(Formatter)]
    [InlineData(ReplicationCodec)]
    public void Implementations_AreListedInProtocolCodecs(string contract)
    {
        GameAssembly game = GameAssembly.Instance;
        TypeDefinition registry = game.FindByName(ProtocolCodecs)
                                  ?? throw new InvalidOperationException($"{ProtocolCodecs} is gone");
        HashSet<string> listed = ReferencedTypes(registry);
        List<TypeDefinition> implementations = game.Types.Where(type => Implements(type, contract)).ToList();
        Assert.NotEmpty(implementations);

        FailureReport report = new($"{contract} implementations missing from {ProtocolCodecs}");
        foreach (TypeDefinition implementation in implementations.Where(type => !listed.Contains(type.FullName)))
        {
            report.Add($"{implementation.FullName} is not listed in ProtocolCodecs");
        }

        report.AssertEmpty();
    }

    // The formatters the MessagePack source generator emits for [MessagePackObject] types live in its own namespace
    private static bool Implements(TypeDefinition type, string contract) =>
        type is { IsAbstract: false, IsInterface: false }
        && GameAssembly.Outermost(type).Namespace.StartsWith(GameNamespace, StringComparison.Ordinal)
        && type.Interfaces
            .Select(implemented => implemented.InterfaceType)
            .OfType<GenericInstanceType>()
            .Any(generic => generic.ElementType.FullName == contract);

    private static HashSet<string> ReferencedTypes(TypeDefinition registry) =>
        registry.Methods
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .Select(instruction => instruction.Operand switch
            {
                FieldReference field => field.DeclaringType.FullName,
                MethodReference { Name: ".ctor" } constructor => constructor.DeclaringType.FullName,
                _ => null,
            })
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
}

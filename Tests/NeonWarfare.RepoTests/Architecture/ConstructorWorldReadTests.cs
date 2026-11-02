using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The container creates every world service eagerly, before any entity exists: on the server the storages are
/// spawned only after that, on a client they arrive with the world snapshot. So a service constructor that reads
/// the world — through a query or a lookup of the entity registry — reads an empty one. Subscribing to
/// <c>IEntityFinder.SpawnedEvent</c> is fine: it reads nothing yet. Only a direct call is caught, not one made
/// through another method.
/// </summary>
[Collection(GameAssembly.Collection)]
public class ConstructorWorldReadTests
{
    // A call through the interface is declared by it, a call on the class by the class
    private static readonly string[] Registries =
    [
        WorldLayers.WorldNamespace + ".Entities.EntityRegistry",
        WorldLayers.WorldNamespace + ".Entities.IEntityFinder",
    ];

    private static readonly string[] RegistryLookups =
        ["GetNode", "TryGetNode", "TryGetNetId", "GetAll", "GetSingle", "Exists"];

    [Fact]
    public void Constructors_DoNotReadTheWorld()
    {
        FailureReport report = new("World service constructors reading the world before it exists");
        GameAssembly game = GameAssembly.Instance;

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.DeclaredLayer(type) is not { } layer)
            {
                continue;
            }

            IEnumerable<MethodDefinition> constructors =
                type.Methods.Where(method => method.IsConstructor && !method.IsStatic && method.HasBody);
            foreach (MethodDefinition constructor in constructors)
            {
                foreach (Instruction instruction in constructor.Body.Instructions)
                {
                    if (instruction.Operand is not MethodReference called || called.Name == ".ctor")
                    {
                        continue;
                    }

                    TypeReference declaring = called.DeclaringType;
                    bool lookup = Registries.Contains(declaring.FullName) && RegistryLookups.Contains(called.Name);
                    bool query = game.Find(declaring) is { } local && WorldLayers.DeclaredLayer(local) == Layer.Query;
                    if (lookup || query)
                    {
                        report.Add($"{GameAssembly.Describe(constructor)}: {layer} calls " +
                                   $"{GameAssembly.ShortName(declaring)}.{called.Name} — read the world when " +
                                   "a method is called, not in the constructor");
                    }
                }
            }
        }

        report.AssertEmpty();
    }
}

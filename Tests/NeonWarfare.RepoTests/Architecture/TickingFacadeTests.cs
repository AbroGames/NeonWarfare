using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// <c>ServerTickLoop</c> ticks every <c>ITickingFacade</c> in the container. The layer is checked here and not only by
/// the collection rules: they let the <c>[Server]</c> tick loop take a collection of <c>[Server]</c> and <c>[Query]</c>
/// services too. A ticking facade keeps no state of its own: a field it fills itself would be lost on a save and
/// missing on a joining peer, so every field holds a constructor dependency or the logger.
/// </summary>
[Collection(GameAssembly.Collection)]
public class TickingFacadeTests
{
    private const string TickingFacade = WorldLayers.WorldNamespace + ".Infra.Server.Tick.ITickingFacade";
    private const string Logger = "Serilog.ILogger";

    private const string FixtureInterface = """
        namespace NeonWarfare.Scenes.Worlds.Infra.Server.Tick
        {
            public interface ITickingFacade { void Tick(); }
        }

        """;

    [Fact]
    public void TickingFacades_AreSimulationFacades() =>
        NotSimulationFacades(GameAssembly.Instance).AssertEmpty();

    [Fact]
    public void TickingFacades_KeepNoStateOfTheirOwn() =>
        OwnState(GameAssembly.Instance).AssertEmpty();

    [Fact]
    public void TickingFacadeOfAnotherLayer_IsReported()
    {
        GameAssembly fixture = GameAssembly.Compile(ArchitectureFixtures.LayerAttributes + FixtureInterface + """
            namespace Fixture
            {
                using NeonWarfare.Scenes.Worlds.Infra.Composition;
                using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

                [SimulationFacade] public class FacadeSimulationFacade : ITickingFacade { public void Tick() { } }
                [Query] public class TickingQuery : ITickingFacade { public void Tick() { } }
            }
            """);

        string failure = Assert.Single(NotSimulationFacades(fixture).Failures);
        Assert.StartsWith("TickingQuery:", failure);
    }

    [Fact]
    public void TickingFacadeWithOwnState_IsReported()
    {
        GameAssembly fixture = GameAssembly.Compile(ArchitectureFixtures.LayerAttributes + FixtureInterface + """
            namespace Fixture
            {
                using NeonWarfare.Scenes.Worlds.Infra.Composition;
                using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

                public class Dependency;

                [SimulationFacade]
                public class PrimarySimulationFacade(Dependency dependency) : ITickingFacade
                {
                    private readonly Serilog.ILogger _log = Serilog.Log.Logger;
                    public void Tick() => System.GC.KeepAlive(dependency);
                }

                [SimulationFacade]
                public class AssignedSimulationFacade : ITickingFacade
                {
                    private readonly Dependency _dependency;
                    public AssignedSimulationFacade(Dependency dependency) { _dependency = dependency; }
                    public void Tick() => System.GC.KeepAlive(_dependency);
                }

                [SimulationFacade]
                public class CounterSimulationFacade : ITickingFacade
                {
                    private int _ticks;
                    public void Tick() => _ticks++;
                }

                [SimulationFacade]
                public class CollectionSimulationFacade : ITickingFacade
                {
                    private readonly System.Collections.Generic.List<int> _pending = new();
                    public void Tick() => _pending.Clear();
                }

                [SimulationFacade]
                public class SetterSimulationFacade : ITickingFacade
                {
                    private int _last;
                    public void Remember(int value) => _last = value;
                    public void Tick() => System.GC.KeepAlive(_last);
                }

                [SimulationFacade]
                public class StaticSimulationFacade : ITickingFacade
                {
                    private static int _ticks;
                    public void Tick() => _ticks++;
                }

                [SimulationFacade]
                public class ByReferenceSimulationFacade : ITickingFacade
                {
                    private int _ticks;
                    public void Tick() => System.Threading.Interlocked.Increment(ref _ticks);
                }

                public readonly struct Span(long start)
                {
                    public long Start() => start;
                }

                [SimulationFacade]
                public class ReadonlyStructSimulationFacade(Span span) : ITickingFacade
                {
                    private readonly Span _span = span;
                    public void Tick() => System.GC.KeepAlive(_span.Start());
                }
            }
            """);

        FailureReport report = OwnState(fixture);

        Assert.Equal(5, report.Failures.Count);
        Assert.Contains(report.Failures, failure => failure.Contains("CounterSimulationFacade"));
        Assert.Contains(report.Failures, failure => failure.Contains("CollectionSimulationFacade"));
        Assert.Contains(report.Failures, failure => failure.Contains("SetterSimulationFacade"));
        Assert.Contains(report.Failures, failure => failure.Contains("StaticSimulationFacade"));
        Assert.Contains(report.Failures, failure => failure.Contains("ByReferenceSimulationFacade"));
    }

    private static FailureReport NotSimulationFacades(GameAssembly game)
    {
        FailureReport report = new("Ticking facades outside the facade layer");

        foreach (TypeDefinition type in TickingFacades(game)
                     .Where(type => WorldLayers.DeclaredLayer(type) != Layer.SimulationFacade))
        {
            report.Add($"{GameAssembly.Describe(type)}: implements ITickingFacade but is not [SimulationFacade]");
        }

        return report;
    }

    /// <summary>
    /// A field is a dependency when only a constructor writes it, and only with a constructor parameter.
    /// A static field is always state, because it outlives the World.
    /// </summary>
    private static FailureReport OwnState(GameAssembly game)
    {
        FailureReport report = new("Fields of ticking facades that hold state of their own");

        foreach (TypeDefinition type in TickingFacades(game))
        {
            HashSet<FieldDefinition> state = [];
            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
            {
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.Code is not (Code.Stfld or Code.Stsfld or Code.Ldflda)
                        || instruction.Operand is not FieldReference written
                        || written.Resolve() is not { } field
                        || field.DeclaringType != type)
                    {
                        continue;
                    }

                    if (IsOwnState(method, instruction, field))
                    {
                        state.Add(field);
                    }
                }
            }

            foreach (FieldDefinition field in state)
            {
                if (!field.IsInitOnly || field.FieldType.FullName != Logger)
                {
                    report.Add($"{GameAssembly.Describe(type)}::{field.Name}: a ticking facade keeps its state in " +
                               "the models, not in a field");
                }
            }
        }

        return report;
    }

    // The address of a readonly field is only read through, because C# passes it by reference only in a constructor
    private static bool IsOwnState(MethodDefinition method, Instruction instruction, FieldDefinition field) =>
        instruction.OpCode.Code switch
        {
            Code.Stsfld => true,
            Code.Ldflda => !field.IsInitOnly && !method.IsConstructor,
            _ => !method.IsConstructor || instruction.Previous is not { } loaded || !LoadsParameter(loaded),
        };

    // ldarg.0 is this, so only the later arguments are constructor parameters
    private static bool LoadsParameter(Instruction instruction) =>
        instruction.OpCode.Code is Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3
        || (instruction.OpCode.Code is Code.Ldarg_S or Code.Ldarg
            && instruction.Operand is ParameterDefinition { Index: >= 0 });

    private static IEnumerable<TypeDefinition> TickingFacades(GameAssembly game)
    {
        // Otherwise a rename of the interface would leave these tests nothing to check
        Assert.NotNull(game.FindByName(TickingFacade));
        return game.Types.Where(type => type is { IsInterface: false, IsAbstract: false }
                                        && WorldLayers.Implements(type, TickingFacade));
    }
}

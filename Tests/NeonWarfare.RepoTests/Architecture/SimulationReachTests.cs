using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The Simulation is what the command handlers call, so it must not reach back to the tick machinery that calls them:
/// with such a cycle the handlers could not be constructor-injected into <c>CommandHandlerRegistry</c>. The layer
/// table allows a Simulation to take any <c>[Server]</c> service, so the check follows the constructor chain, not one
/// step of it.
/// </summary>
[Collection(GameAssembly.Collection)]
public class SimulationReachTests
{
    private const string Server = WorldLayers.WorldNamespace + ".Infra.Server";

    private static readonly string[] TickMachinery =
    [
        Server + ".Tick.ServerTickLoop",
        Server + ".Commands.CommandDispatcher",
        Server + ".Commands.CommandInbox",
        Server + ".Commands.CommandHandlerRegistry",
        Server + ".Peers.PeerSessions",
    ];

    [Fact]
    public void SimulationConstructors_DoNotReachTheTickMachinery()
    {
        FailureReport report = new("Simulation services reaching the tick machinery through their constructors");
        GameAssembly game = GameAssembly.Instance;
        // Otherwise a rename would leave the check nothing to find
        foreach (string name in TickMachinery.Where(name => game.FindByName(name) == null))
        {
            report.Add($"{name} no longer exists");
        }

        foreach (TypeDefinition type in game.Types.Where(type =>
                     WorldLayers.DeclaredLayer(type) is Layer.Simulation or Layer.SimulationFacade))
        {
            if (FindPath(game, type) is { } path)
            {
                report.Add(string.Join(" → ", path.Select(GameAssembly.ShortName)));
            }
        }

        report.AssertEmpty();
    }

    private static List<TypeDefinition>? FindPath(GameAssembly game, TypeDefinition start)
    {
        Dictionary<string, TypeDefinition?> cameFrom = new(StringComparer.Ordinal) { [start.FullName] = null };
        Queue<TypeDefinition> queue = new([start]);
        while (queue.TryDequeue(out TypeDefinition? current))
        {
            if (TickMachinery.Contains(current.FullName))
            {
                List<TypeDefinition> path = [];
                for (TypeDefinition? step = current; step != null; step = cameFrom[step.FullName])
                {
                    path.Add(step);
                }
                path.Reverse();
                return path;
            }

            IEnumerable<TypeDefinition> next = current.Methods
                .Where(method => method.IsConstructor && !method.IsStatic && method.IsPublic)
                .SelectMany(constructor => constructor.Parameters)
                .Select(parameter => game.Find(parameter.ParameterType))
                .OfType<TypeDefinition>();
            foreach (TypeDefinition dependency in next
                         .Where(dependency => cameFrom.TryAdd(dependency.FullName, current)))
            {
                queue.Enqueue(dependency);
            }
        }

        return null;
    }
}

using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The Simulation changes state only inside the tick: the server sends strictly at the end of it, so
/// anything deferred past it — a deferred call, a timer, a tween, an <c>await</c> — lands between two
/// ticks, outside the outbox and the replicated delta of either. Time-based rules go into <c>Tick()</c>.
/// </summary>
[Collection(GameAssembly.Collection)]
public class SimulationTimingTests
{
    private static readonly string[] DeferringGodotMethods =
    [
        "CallDeferred", "CallDeferredThreadGroup", "SetDeferred", "SetDeferredThreadGroup",
        "CreateTimer", "CreateTween", "ToSignal",
    ];

    private static readonly string[] DeferringGodotTypes = ["Godot.Timer", "Godot.Tween"];

    private static readonly string[] AsyncStateMachineAttributes =
    [
        "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
        "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
    ];

    [Fact]
    public void SimulationGroup_DefersNothingPastTheTick()
    {
        FailureReport report = new("Simulation work deferred past the tick");
        GameAssembly game = GameAssembly.Instance;

        foreach (TypeDefinition type in game.Types)
        {
            if (WorldLayers.LayerOf(type) is not { } layer || !WorldLayers.SimulationGroup.Contains(layer))
            {
                continue;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (method.CustomAttributes.Any(a => AsyncStateMachineAttributes.Contains(a.AttributeType.FullName)))
                {
                    report.Add($"{GameAssembly.Describe(method)}: {layer} has an async method");
                }

                if (!method.HasBody)
                {
                    continue;
                }

                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is MethodReference called
                        && GameAssembly.Outermost(called.DeclaringType).Namespace == "Godot"
                        && DeferringGodotMethods.Contains(called.Name))
                    {
                        report.Add($"{GameAssembly.Describe(method)}: {layer} calls " +
                                   $"{GameAssembly.ShortName(called.DeclaringType)}.{called.Name}");
                    }
                }
            }

            IEnumerable<string> typeReferences = TypeReferences.Of(type)
                .Where(site => DeferringGodotTypes.Contains(site.Type.FullName))
                .Select(site => $"{GameAssembly.Describe(site.From)}: {layer} refers to {site.Type.FullName}")
                .Distinct();
            foreach (string violation in typeReferences)
            {
                report.Add(violation);
            }
        }

        report.AssertEmpty();
    }
}

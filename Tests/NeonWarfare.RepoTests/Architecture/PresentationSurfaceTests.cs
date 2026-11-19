using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// A Presentation changes only in its event handlers; nodes and widgets only read it, in <c>_Process</c>. Whether a
/// method mutates cannot be told from IL — <c>_entries.Enqueue(…)</c> is a field read and a call, as a getter
/// returning <c>_entries</c> is — so the rule is the shape instead: outwards only property getters, and a getter
/// reaches neither a field write nor what the event handlers run. That also rules out a lazy cache and a pure helper
/// shared by a getter and a handler: the price of a rule that needs no exceptions.
/// </summary>
[Collection(GameAssembly.Collection)]
public class PresentationSurfaceTests
{
    /// <summary>
    /// The mailbox is the channel between the handlers and the widgets: <c>Post</c> and <c>Read&lt;T&gt;</c> are its
    /// whole point. Who may post is checked by <c>LayerReferenceTests</c>.
    /// </summary>
    private static readonly string[] OpenPresentations =
    [
        WorldLayers.WorldNamespace + ".Infra.Presentation.HudMailbox",
    ];

    [Fact]
    public void Presentations_ExposeOnlyGetters()
    {
        FailureReport report = new("Presentation members a node could change it through");

        foreach (TypeDefinition type in Presentations())
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.IsPrivate && !method.IsConstructor && !method.IsGetter)
                {
                    report.Add($"{GameAssembly.Describe(method)}: is not private and not a property getter");
                }
            }

            foreach (FieldDefinition field in type.Fields.Where(field => !field.IsPrivate))
            {
                report.Add($"{GameAssembly.Describe(field)}: is a field that is not private");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void PresentationGetters_DoNotMutate()
    {
        FailureReport report = new("Presentation getters that change it");

        foreach (TypeDefinition type in Presentations())
        {
            HashSet<MethodDefinition> handlerCode =
                ReachableWithin(type, type.Methods.Where(WorldLayers.IsEventHandler));
            foreach (MethodDefinition getter in type.Methods.Where(method => method.IsGetter))
            {
                foreach (MethodDefinition reached in ReachableWithin(type, [getter]))
                {
                    string where = GameAssembly.Describe(getter);
                    if (handlerCode.Contains(reached))
                    {
                        report.Add($"{where}: reaches {reached.Name}, which the event handlers run");
                    }

                    foreach (FieldReference field in WrittenFields(reached, type))
                    {
                        report.Add($"{where}: writes {field.Name} through {reached.Name}");
                    }
                }
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void OpenPresentations_AreNotStale() =>
        CrossCheck.AssertExemptionsExist(
            nameof(OpenPresentations),
            OpenPresentations,
            name => GameAssembly.Instance.FindByName(name) is { } type
                    && WorldLayers.DeclaredLayer(type) == Layer.Presentation,
            "no such [Presentation] in the game assembly");

    private static List<TypeDefinition> Presentations()
    {
        List<TypeDefinition> presentations = GameAssembly.Instance.Types
            .Where(type => WorldLayers.DeclaredLayer(type) == Layer.Presentation
                           && !OpenPresentations.Contains(type.FullName))
            .ToList();
        // Without them a rename would leave the rule nothing to check
        Assert.NotEmpty(presentations);
        return presentations;
    }

    /// <summary>
    /// The starting methods and every method of the type, its lambdas included, they call or take as a delegate,
    /// at any depth. Constructors are left out: they run before any getter or handler can.
    /// </summary>
    private static HashSet<MethodDefinition> ReachableWithin(TypeDefinition type, IEnumerable<MethodDefinition> start)
    {
        HashSet<MethodDefinition> reached = [];
        Queue<MethodDefinition> pending = new(start);
        while (pending.TryDequeue(out MethodDefinition? method))
        {
            if (!reached.Add(method))
            {
                continue;
            }

            foreach (MethodCalls.Call call in MethodCalls.Of(method))
            {
                bool own = GameAssembly.SelfAndEnclosing(call.Target.DeclaringType).Contains(type);
                if (own && !call.Target.IsConstructor)
                {
                    pending.Enqueue(call.Target);
                }
            }
        }

        return reached;
    }

    private static IEnumerable<FieldReference> WrittenFields(MethodDefinition method, TypeDefinition type) =>
        !method.HasBody
            ? []
            : method.Body.Instructions
                .Where(instruction => instruction.OpCode.Code is Code.Stfld or Code.Stsfld)
                .Select(instruction => instruction.Operand)
                .OfType<FieldReference>()
                .Where(field => field.DeclaringType.GetElementType().FullName == type.FullName);
}

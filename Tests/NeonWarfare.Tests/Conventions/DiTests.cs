using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// The two halves of the injection contract from Docs/Dependency-injection.md. Both fail the same way:
/// silently. Without <c>Di.Process(this)</c> every annotated field simply stays <c>null</c>, and the
/// compiler has nothing to say about it — the game dies later, in a place that has no obvious connection
/// to the field that was never filled.
/// </summary>
public class DiTests
{
    private const string ProcessMethod = "Process";

    /// <summary>The injector, either through <c>Services.Di</c> or through the global using static.</summary>
    private static readonly string[] AllowedReceivers = ["Di", "Services.Di"];

    /// <summary>
    /// The attributes that need an injector run. <c>[NotNull]</c> is deliberately not one of them: it
    /// validates an <c>[Export]</c> filled in by the Godot editor, and in WorldMultiplayerSpawner.cs the
    /// name even resolves to System.Diagnostics.CodeAnalysis instead.
    /// </summary>
    private static readonly string[] InjectionAttributes = ["Child", "Parent", "SceneService", "Logger"];

    /// <summary>
    /// "Write <c>Di.Process(this)</c> as the first line in <c>_Ready()</c>" from Docs/Code-style.md,
    /// checked as the position rather than the method name: BaseRootStarter is not a node and runs it
    /// first thing in <c>Init()</c>, which is the same rule applied to a class that has no _Ready().
    /// Anything the method does before injecting is working with fields that are still null.
    /// </summary>
    [Fact]
    public void DiProcess_IsTheFirstStatementOfItsMethod()
    {
        FailureReport report = new("Di.Process(this) calls that are not the first statement");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            foreach (InvocationExpressionSyntax invocation in DiProcessCalls(file))
            {
                ExpressionStatementSyntax? statement =
                    invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                BaseMethodDeclarationSyntax? owner =
                    invocation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();

                if (statement is null || owner?.Body is null)
                {
                    report.Add($"{file.Describe(invocation)}: expected a statement inside a method or " +
                               $"constructor body");
                    continue;
                }

                if (owner.Body.Statements.FirstOrDefault() != statement)
                {
                    report.Add($"{file.Describe(invocation)}: Di.Process(this) must come first, " +
                               $"everything above it reads fields that are still null");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// A class that annotates fields but never runs the injector. The call may live in a base class —
    /// ClientRootStarter and DedicatedServerRootStarter carry a <c>[Logger]</c> and inherit the
    /// <c>Di.Process(this)</c> that BaseRootStarter.Init() makes.
    /// </summary>
    [Fact]
    public void TypesWithInjectedMembers_CallDiProcess()
    {
        IReadOnlyDictionary<string, TypeFacts> types = TypesByName();
        FailureReport report = new("Types with injected members that never run Di.Process(this)");

        foreach (TypeFacts type in types.Values.OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            if (type.InjectedMembers.Count == 0 || RunsDiProcess(type, types))
            {
                continue;
            }

            report.Add($"{type.Location}: {type.Name} declares {string.Join(", ", type.InjectedMembers)} " +
                       $"but neither it nor its base types call Di.Process(this)");
        }

        report.AssertEmpty();
    }

    /// <summary>Calls of the form <c>Di.Process(this)</c>, whichever receiver they are written through.</summary>
    private static IEnumerable<InvocationExpressionSyntax> DiProcessCalls(CSharpFile file) =>
        file.Nodes<InvocationExpressionSyntax>()
            .Where(invocation =>
                CSharpFile.CalledName(invocation) == ProcessMethod
                && AllowedReceivers.Contains(CSharpFile.ReceiverOf(invocation))
                && invocation.ArgumentList.Arguments.Count == 1
                && invocation.ArgumentList.Arguments[0].Expression is ThisExpressionSyntax);

    /// <summary>
    /// Every declared type, by its bare name. Names are enough here: there is no semantic model without a
    /// reference to the game project, and the repository has no two types sharing a name.
    /// </summary>
    private static IReadOnlyDictionary<string, TypeFacts> TypesByName()
    {
        Dictionary<string, TypeFacts> types = new(StringComparer.Ordinal);

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            IReadOnlyList<AttributedMember> injected = file.MembersWith(InjectionAttributes).ToList();
            IReadOnlyList<InvocationExpressionSyntax> calls = DiProcessCalls(file).ToList();

            foreach (TypeDeclarationSyntax declaration in file.Nodes<TypeDeclarationSyntax>())
            {
                string name = declaration.Identifier.ValueText;
                if (!types.TryGetValue(name, out TypeFacts? facts))
                {
                    // A partial type is declared in several places; each of them adds to the same facts.
                    facts = new TypeFacts(name, file.Describe(declaration));
                    types[name] = facts;
                }

                foreach (string baseType in BaseTypeNames(declaration))
                {
                    facts.BaseTypes.Add(baseType);
                }

                foreach (AttributedMember member in injected
                             .Where(member => member.DeclaringType == declaration))
                {
                    facts.InjectedMembers.Add(
                        $"[{CSharpFile.AttributeName(member.Attribute)}] {member.Name}");
                }

                facts.CallsDiProcess |= calls.Any(call => CSharpFile.DeclaringType(call) == declaration);
            }
        }

        return types;
    }

    private static IEnumerable<string> BaseTypeNames(TypeDeclarationSyntax declaration) =>
        declaration.BaseList?.Types
            .Select(baseType => baseType.Type)
            .Select(type => type switch
            {
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                SimpleNameSyntax simple => simple.Identifier.ValueText,
                _ => type.ToString(),
            }) ?? [];

    /// <summary>
    /// The type itself or any of its base types declared in this repository. Base types that come from
    /// KludgeBox or Godot simply are not in the map and end the walk.
    /// </summary>
    private static bool RunsDiProcess(TypeFacts type, IReadOnlyDictionary<string, TypeFacts> types)
    {
        HashSet<string> visited = new(StringComparer.Ordinal);
        Queue<TypeFacts> pending = new([type]);

        while (pending.Count > 0)
        {
            TypeFacts current = pending.Dequeue();
            if (!visited.Add(current.Name))
            {
                continue;
            }

            if (current.CallsDiProcess)
            {
                return true;
            }

            foreach (string baseType in current.BaseTypes)
            {
                if (types.TryGetValue(baseType, out TypeFacts? declared))
                {
                    pending.Enqueue(declared);
                }
            }
        }

        return false;
    }

    private sealed class TypeFacts
    {
        public TypeFacts(string name, string location)
        {
            Name = name;
            Location = location;
        }

        public string Name { get; }

        /// <summary>Path and line of the first declaration seen, for the failure message.</summary>
        public string Location { get; }

        public HashSet<string> BaseTypes { get; } = new(StringComparer.Ordinal);

        public List<string> InjectedMembers { get; } = [];

        public bool CallsDiProcess { get; set; }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// The input map lives in two places that nothing connects: the actions are configured in project.godot
/// and named in Keys.cs as plain strings. Godot answers a request for an action it does not know with a
/// silent "not pressed", so a typo or a deleted binding turns into controls that quietly do nothing.
/// </summary>
public class InputActionTests
{
    /// <summary>
    /// Godot's own actions. They come from the engine defaults rather than from project.godot, so they
    /// are correct precisely by not being declared there.
    /// </summary>
    private const string BuiltInActionPrefix = "ui_";

    [Fact]
    public void DeclaredActions_ExistInTheInputMap()
    {
        IReadOnlySet<string> configured = GodotProjectFile.Current.InputActions.ToHashSet(StringComparer.Ordinal);
        FailureReport report = new("Actions named in Keys.cs that project.godot does not declare");

        foreach ((LiteralExpressionSyntax literal, string action) in DeclaredActions())
        {
            if (action.StartsWith(BuiltInActionPrefix, StringComparison.Ordinal) || configured.Contains(action))
            {
                continue;
            }

            report.Add($"{Keys.Describe(literal)}: '{action}' — either add the binding to project.godot " +
                       $"or drop the declaration");
        }

        report.AssertEmpty();
    }

    [Fact]
    public void ConfiguredActions_AreDeclaredInKeys()
    {
        IReadOnlySet<string> declared = DeclaredActions()
            .Select(declaration => declaration.Action)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new("Actions bound in project.godot that Keys.cs does not name");

        foreach (string action in GodotProjectFile.Current.InputActions)
        {
            if (!declared.Contains(action))
            {
                report.Add($"'{action}' — a binding nothing in the code can reach");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// Every string in Keys.cs is an action name: the file holds nothing else. Reading the literals
    /// rather than the field names is the point — the field is <c>AttackPrimary</c> and the action is
    /// <c>KeyAttackPrimary</c>, and it is the string that reaches Godot.
    /// </summary>
    private static IEnumerable<(LiteralExpressionSyntax Literal, string Action)> DeclaredActions() =>
        Keys.Nodes<LiteralExpressionSyntax>()
            .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
            .Select(literal => (literal, literal.Token.ValueText));

    private static CSharpFile Keys => CSharpFile.Load(RepositoryPaths.InputActionsPath);
}

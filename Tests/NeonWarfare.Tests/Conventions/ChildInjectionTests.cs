using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// "<c>[Child]</c> matches by field name, so renaming a field requires renaming the node in the scene
/// (and vice versa)" from Docs/Dependency-injection.md. Nothing in the toolchain connects the two: the
/// field name is C#, the node name is a string inside a .tscn, and the mismatch turns into a
/// NotFoundException on the first run of whichever screen happens to use that node.
/// <br/>
/// The rule is read from KludgeBox's ChildInjectionRequestScanner: the name looked for is the attribute's
/// own argument if it has one and the member name otherwise, matched verbatim, and the search goes
/// through the whole subtree unless DeepScan is turned off.
/// </summary>
public class ChildInjectionTests
{
    private const string ChildAttribute = "Child";

    /// <summary>Selects the node by its type, which leaves the member name free to be anything.</summary>
    private const string SearchByType = "By.Type";

    [Fact]
    public void ChildMembers_MatchANodeInTheScene()
    {
        IReadOnlyDictionary<string, IReadOnlyList<ScriptAttachment>> attachments =
            SceneFile.AttachmentsByScript();
        FailureReport report = new("[Child] members that no node in the scene answers to");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            IReadOnlyList<AttributedMember> members = file.MembersWith(ChildAttribute)
                .Where(member => !SearchesByType(member.Attribute))
                .ToList();

            if (members.Count == 0)
            {
                continue;
            }

            if (!attachments.TryGetValue(file.RelativePath, out IReadOnlyList<ScriptAttachment>? attached))
            {
                // Not an exemption to add quietly: a node built from code still needs its children to
                // exist, and there is nothing left to check them against.
                report.Add($"{file.RelativePath}: has [Child] members " +
                           $"({string.Join(", ", members.Select(member => member.Name))}) but no .tscn " +
                           $"attaches this script to a node");
                continue;
            }

            foreach (ScriptAttachment attachment in attached)
            {
                CheckAgainstScene(file, members, attachment, report);
            }
        }

        report.AssertEmpty();
    }

    private static void CheckAgainstScene(
        CSharpFile file,
        IEnumerable<AttributedMember> members,
        ScriptAttachment attachment,
        FailureReport report)
    {
        foreach (AttributedMember member in members)
        {
            string wanted = ExplicitName(member.Attribute) ?? member.Name;
            IReadOnlyList<SceneNode> reachable = (ScansDeep(member.Attribute)
                    ? attachment.Scene.Descendants(attachment.Node)
                    : attachment.Scene.Children(attachment.Node))
                .ToList();

            IReadOnlyList<SceneNode> matches = reachable
                .Where(node => string.Equals(node.Name, wanted, StringComparison.Ordinal))
                .ToList();

            string where = $"{file.Describe(member.Declaration)}: '{wanted}' under " +
                           $"{attachment.Scene.RelativePath} node {attachment.Node.Name}";

            if (matches.Count == 0)
            {
                report.Add($"{where} — no such node. Reachable: " +
                           $"{string.Join(", ", reachable.Select(node => node.Name).Order(StringComparer.Ordinal))}");
                continue;
            }

            // FindChild returns the first hit of a breadth-first walk, so a duplicated name means the
            // member is filled with whichever node the tree order happens to reach first.
            if (matches.Count > 1)
            {
                report.Add($"{where} — {matches.Count} nodes share this name " +
                           $"(lines {string.Join(", ", matches.Select(node => node.Line))}), the injection " +
                           $"is ambiguous");
            }
        }
    }

    private static bool SearchesByType(AttributeSyntax attribute) =>
        attribute.ArgumentList?.Arguments
            .Any(argument => argument.Expression.ToString() == SearchByType) ?? false;

    /// <summary>The name the attribute overrides the member name with, as in <c>[Child(By.Name, "Foo")]</c>.</summary>
    private static string? ExplicitName(AttributeSyntax attribute) =>
        attribute.ArgumentList?.Arguments
            .Select(argument => argument.Expression)
            .OfType<LiteralExpressionSyntax>()
            .FirstOrDefault(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
            ?.Token.ValueText;

    /// <summary>DeepScan is on unless the attribute passes <c>false</c> for it.</summary>
    private static bool ScansDeep(AttributeSyntax attribute) =>
        !(attribute.ArgumentList?.Arguments
            .Any(argument => argument.Expression.IsKind(SyntaxKind.FalseLiteralExpression)) ?? false);
}

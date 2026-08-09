using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// "Check the role only through <c>Net.*</c>, not through <c>GetMultiplayer().IsServer()</c> directly"
/// from Docs/Code-style.md. The two answers differ: <c>Net.IsServer()</c> is also true in the main menu
/// and in a single-player game, where there is no outer controller at all, while Godot's own flag is
/// only about the multiplayer peer. Asking Godot directly therefore works over the network and breaks
/// single-player — the kind of bug that survives every build and most manual testing.
/// </summary>
public class RoleCheckTests
{
    private static readonly string[] RoleCheckMethods = ["IsServer", "IsClient"];

    /// <summary>The service the role must be asked from — <c>Services.Net</c>, or <c>Net</c> through the
    /// global using static in Scripts/GlobalUsings.cs.</summary>
    private static readonly string[] AllowedReceivers = ["Net", "Services.Net"];

    private const string PeerIdMethod = "GetUniqueId";

    [Fact]
    public void RoleCheck_GoesThroughNetService()
    {
        FailureReport report = new("Role checks that bypass Net.*");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            foreach (InvocationExpressionSyntax invocation in file.Nodes<InvocationExpressionSyntax>())
            {
                string called = CSharpFile.CalledName(invocation);
                if (!RoleCheckMethods.Contains(called))
                {
                    continue;
                }

                string receiver = CSharpFile.ReceiverOf(invocation);
                if (!AllowedReceivers.Contains(receiver))
                {
                    string through = receiver.Length == 0 ? "no receiver" : $"'{receiver}'";
                    report.Add($"{file.Describe(invocation)}: {called}() is called on {through}, " +
                               $"use Net.{called}()");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The other way to hand-roll the same check: peer id 1 is the server in Godot. Taking the own peer
    /// id is fine — WorldFacadeService and Network both do it — comparing it with a number is not.
    /// </summary>
    [Fact]
    public void PeerId_IsNotComparedWithLiteral()
    {
        FailureReport report = new("Role checks written as a peer id comparison");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            foreach (BinaryExpressionSyntax comparison in file.Nodes<BinaryExpressionSyntax>())
            {
                if (!comparison.IsKind(SyntaxKind.EqualsExpression)
                    && !comparison.IsKind(SyntaxKind.NotEqualsExpression))
                {
                    continue;
                }

                if (IsPeerIdComparison(comparison.Left, comparison.Right)
                    || IsPeerIdComparison(comparison.Right, comparison.Left))
                {
                    report.Add($"{file.Describe(comparison)}: '{comparison}' — use Net.IsServer()");
                }
            }
        }

        report.AssertEmpty();
    }

    private static bool IsPeerIdComparison(ExpressionSyntax candidate, ExpressionSyntax other) =>
        candidate is InvocationExpressionSyntax invocation
        && CSharpFile.CalledName(invocation) == PeerIdMethod
        && other.IsKind(SyntaxKind.NumericLiteralExpression);
}

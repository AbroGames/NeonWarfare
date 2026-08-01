using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// The RPC shape from Docs/Code-style.md: a public wrapper plus a private <c>*Rpc</c> method marked
/// <c>[Rpc(...)]</c> with the mode spelled out, and the <c>*Rpc</c> method reached only through that
/// wrapper. Godot resolves RPC targets by name at run time, so every mistake here — a public target, a
/// missing attribute, a second caller bypassing the wrapper — compiles and only shows up over the
/// network.
/// </summary>
public class RpcConventionTests
{
    private const string RpcSuffix = "Rpc";
    private const string RpcAttribute = "Rpc";

    /// <summary>The class Godot generates for method names; RPC targets are referenced through it.</summary>
    private const string MethodNameClass = "MethodName";

    [Fact]
    public void RpcMethod_IsPrivate()
    {
        FailureReport report = new("RPC targets that are not private");

        foreach ((CSharpFile file, MethodDeclarationSyntax method) in RpcMethods())
        {
            if (!method.Modifiers.Any(modifier => modifier.ValueText == "private"))
            {
                string modifiers = string.Join(" ", method.Modifiers.Select(modifier => modifier.ValueText));
                report.Add($"{file.Describe(method)}: {method.Identifier.ValueText} is '{modifiers}'");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void RpcMethod_HasRpcAttributeWithExplicitMode()
    {
        FailureReport report = new("RPC targets without a complete [Rpc(...)] attribute");

        foreach ((CSharpFile file, MethodDeclarationSyntax method) in RpcMethods())
        {
            AttributeSyntax? attribute = RpcAttributeOf(method);
            if (attribute is null)
            {
                report.Add($"{file.Describe(method)}: {method.Identifier.ValueText} has no [{RpcAttribute}]");
                continue;
            }

            // [Rpc] with no arguments leaves the mode at Godot's default instead of stating it.
            if (attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count == 0)
            {
                report.Add(
                    $"{file.Describe(attribute)}: {method.Identifier.ValueText} must state the mode, " +
                    $"for example [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void RpcAttribute_IsOnRpcSuffixedMethod()
    {
        FailureReport report = new($"[{RpcAttribute}] on methods whose name does not end with '{RpcSuffix}'");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            foreach (AttributeSyntax attribute in file.Nodes<AttributeSyntax>()
                         .Where(attribute => attribute.Name.ToString() == RpcAttribute))
            {
                MethodDeclarationSyntax? method = CSharpFile.EnclosingMethod(attribute);
                if (method is null)
                {
                    report.Add($"{file.Describe(attribute)}: not attached to a method");
                    continue;
                }

                if (!IsRpcTarget(method))
                {
                    report.Add($"{file.Describe(method)}: rename {method.Identifier.ValueText} to " +
                               $"{method.Identifier.ValueText}{RpcSuffix}");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The wrapper is the single entry point: the side checks and the choice between <c>Rpc</c> and
    /// <c>RpcId</c> live there, so a second caller reaching for the target directly silently skips them.
    /// The wrapper is found by name rather than by position, because not every one of them is the
    /// one-line form — see CharacterSynchronizer_Controller and PlayerDataStorage.
    /// </summary>
    [Fact]
    public void RpcMethod_IsCalledOnlyFromItsWrapper()
    {
        List<(CSharpFile File, MemberAccessExpressionSyntax Reference)> references = CSharpFile.LoadAll()
            .SelectMany(file => file.Nodes<MemberAccessExpressionSyntax>()
                .Where(access => access.Expression.ToString() == MethodNameClass)
                .Where(access => access.Name.Identifier.ValueText.EndsWith(RpcSuffix, StringComparison.Ordinal))
                .Select(access => (File: file, Reference: access)))
            .ToList();

        FailureReport report = new($"RPC targets not called exactly once, from their wrapper");

        foreach ((CSharpFile file, MethodDeclarationSyntax method) in RpcMethods())
        {
            string target = method.Identifier.ValueText;
            string wrapper = target[..^RpcSuffix.Length];
            List<(CSharpFile File, MemberAccessExpressionSyntax Reference)> calls = references
                .Where(reference => reference.Reference.Name.Identifier.ValueText == target)
                .ToList();

            if (calls.Count == 0)
            {
                report.Add($"{file.Describe(method)}: {target} is never called through " +
                           $"{MethodNameClass}.{target} — add a wrapper {wrapper}(...)");
                continue;
            }

            if (calls.Count > 1)
            {
                string places = string.Join(", ", calls.Select(call => call.File.Describe(call.Reference)));
                report.Add($"{file.Describe(method)}: {target} is called from {calls.Count} places " +
                           $"({places}) — all calls must go through {wrapper}(...)");
                continue;
            }

            (CSharpFile callFile, MemberAccessExpressionSyntax reference) = calls[0];
            string? callerName = CSharpFile.EnclosingMethod(reference)?.Identifier.ValueText;
            if (callerName != wrapper)
            {
                report.Add($"{callFile.Describe(reference)}: {target} is called from " +
                           $"'{callerName ?? "outside any method"}', expected the wrapper '{wrapper}'");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>Every method whose name marks it as an RPC target.</summary>
    private static IReadOnlyList<(CSharpFile File, MethodDeclarationSyntax Method)> RpcMethods() =>
        CSharpFile.LoadAll()
            .SelectMany(file => file.Nodes<MethodDeclarationSyntax>()
                .Where(IsRpcTarget)
                .Select(method => (File: file, Method: method)))
            .ToList();

    private static bool IsRpcTarget(MethodDeclarationSyntax method) =>
        method.Identifier.ValueText.Length > RpcSuffix.Length
        && method.Identifier.ValueText.EndsWith(RpcSuffix, StringComparison.Ordinal);

    private static AttributeSyntax? RpcAttributeOf(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => attribute.Name.ToString() == RpcAttribute);
}

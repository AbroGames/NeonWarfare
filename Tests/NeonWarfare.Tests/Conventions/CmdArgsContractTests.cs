using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// The contract Docs/Cli-args.md states: the arguments are declared in Scripts/Content/CmdArgs/, they
/// are parsed only in the Root starters and only through the ready CmdArgsService, and from there
/// travel as ordinary parameters. Nothing stops any node from asking the OS for the command line on its
/// own, which is exactly how a flag ends up with two different meanings in two places.
/// </summary>
public class CmdArgsContractTests
{
    private const string FlagPrefix = "--";

    private const string CmdArgsServiceType = "CmdArgsService";

    /// <summary>The parsing surface of CmdArgsService. LogCmdArgs is deliberately absent: it only
    /// writes the arguments to the log and is called from BaseRootStarter.</summary>
    private static readonly string[] ParsingMethods =
        ["ContainsInCmdArgs", "GetStringFromCmdArgs", "GetIntFromCmdArgs"];

    private const string FromCmdMethod = "GetFromCmd";

    private static readonly string[] CommandLineMethods = ["GetCmdlineArgs", "GetCmdlineUserArgs"];

    /// <summary>
    /// The one place allowed to read the command line without the service: ChooseStarter decides which
    /// starter to build, so neither a starter nor its CmdArgsService exists yet.
    /// </summary>
    private static readonly string[] MayReadCommandLineDirectly =
        ["Scenes/Root/Starters/RootStarterManager.cs"];

    [Fact]
    public void Flags_AreDeclaredOnlyInCmdArgs()
    {
        FailureReport report = new(
            "Command-line flags written outside Scripts/Content/CmdArgs/ — reference the constant instead");

        foreach (CSharpFile file in OutsideCmdArgs())
        {
            foreach (LiteralExpressionSyntax literal in file.Nodes<LiteralExpressionSyntax>()
                         .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                         .Where(literal => literal.Token.ValueText.StartsWith(FlagPrefix, StringComparison.Ordinal)))
            {
                report.Add($"{file.Describe(literal)}: '{literal.Token.ValueText}'");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The service is deliberately kept out of the global Services registry — see the comment in
    /// BaseRootStarter — so naming the type anywhere else means someone is about to parse arguments in
    /// a second place. Checking every mention rather than only <c>new CmdArgsService()</c> also covers
    /// the target-typed <c>new()</c> the field actually uses.
    /// </summary>
    [Fact]
    public void CmdArgsService_IsUsedOnlyInRootStartersAndCmdArgs()
    {
        FailureReport report = new(
            $"{CmdArgsServiceType} referenced outside Scenes/Root/Starters/ and Scripts/Content/CmdArgs/");

        foreach (CSharpFile file in CSharpFile.LoadAll()
                     .Where(file => !IsInRootStarters(file) && !IsInCmdArgs(file)))
        {
            foreach (SyntaxToken mention in file.Root.DescendantTokens()
                         .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                         .Where(token => token.ValueText == CmdArgsServiceType))
            {
                report.Add($"{file.RelativePath}:{file.LineOf(mention.Parent!)}");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void CmdArgsParsing_HappensOnlyInCmdArgs()
    {
        FailureReport report = new(
            "Command-line arguments read outside Scripts/Content/CmdArgs/ — add a field to *Args instead");

        foreach ((CSharpFile file, InvocationExpressionSyntax invocation) in
                 Calls(OutsideCmdArgs(), ParsingMethods))
        {
            report.Add($"{file.Describe(invocation)}: {CSharpFile.CalledName(invocation)}(...)");
        }

        report.AssertEmpty();
    }

    [Fact]
    public void GetFromCmd_IsCalledOnlyFromRootStarters()
    {
        FailureReport report = new($"{FromCmdMethod} called outside the Root starters");

        IEnumerable<CSharpFile> elsewhere = CSharpFile.LoadAll()
            .Where(file => !IsInRootStarters(file) && !IsInCmdArgs(file));

        foreach ((CSharpFile file, InvocationExpressionSyntax invocation) in Calls(elsewhere, [FromCmdMethod]))
        {
            report.Add($"{file.Describe(invocation)}: arguments must arrive as ordinary parameters");
        }

        report.AssertEmpty();
    }

    [Fact]
    public void OsCmdlineArgs_IsNotReadDirectly()
    {
        FailureReport report = new("OS command line read without CmdArgsService");

        IEnumerable<CSharpFile> checked_ = CSharpFile.LoadAll()
            .Where(file => !MayReadCommandLineDirectly.Contains(file.RelativePath));

        foreach ((CSharpFile file, InvocationExpressionSyntax invocation) in Calls(checked_, CommandLineMethods))
        {
            report.Add($"{file.Describe(invocation)}: {CSharpFile.CalledName(invocation)}() — " +
                       $"take the value from the *Args record passed down from the RootStarter");
        }

        report.AssertEmpty();
    }

    private static IEnumerable<CSharpFile> OutsideCmdArgs() =>
        CSharpFile.LoadAll().Where(file => !IsInCmdArgs(file));

    private static bool IsInCmdArgs(CSharpFile file) =>
        RepositoryPaths.IsInside(file.Path, RepositoryPaths.CmdArgsDirectory);

    private static bool IsInRootStarters(CSharpFile file) =>
        RepositoryPaths.IsInside(file.Path, RepositoryPaths.RootStartersDirectory);

    private static IEnumerable<(CSharpFile File, InvocationExpressionSyntax Invocation)> Calls(
        IEnumerable<CSharpFile> files, IReadOnlyCollection<string> methodNames) =>
        files.SelectMany(file => file.Nodes<InvocationExpressionSyntax>()
            .Where(invocation => methodNames.Contains(CSharpFile.CalledName(invocation)))
            .Select(invocation => (File: file, Invocation: invocation)));
}

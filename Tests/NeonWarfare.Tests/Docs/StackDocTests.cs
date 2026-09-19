using System.Text.RegularExpressions;
using System.Xml.Linq;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Docs/Stack.md lists what the projects depend on and what each dependency is for. Adding a
/// package is one line in a .csproj and nothing asks for the other half — the table is the only place
/// that says why the package is there at all.
/// </summary>
public class StackDocTests
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>What "the same three xUnit packages" of the smoke test paragraph refers to.</summary>
    private static readonly string[] XunitPackages =
        ["xunit.v3", "xunit.runner.visualstudio", "Microsoft.NET.Test.Sdk"];

    private const string SmokeProjectClaim = "takes the same three xUnit packages and nothing else";

    private const string DocumentName = "Stack.md";

    private const string PackagesHeading = "Stack and dependencies";

    private const string ProjectExtension = ".csproj";

    private const int PackageColumn = 0;

    [Fact]
    public void GameProjectPackages_MatchTheDocument()
    {
        AssertPackagesMatch(RepositoryPaths.GameProjectPath);
    }

    [Fact]
    public void TestProjectPackages_MatchTheDocument()
    {
        AssertPackagesMatch(RepositoryPaths.TestProjectPath);
    }

    [Fact]
    public void GameTestProjectPackages_MatchTheDocument()
    {
        AssertPackagesMatch(RepositoryPaths.GameTestProjectPath);
    }

    [Fact]
    public void SmokeTestProjectPackages_AreTheXunitPackagesOnly()
    {
        string smokeProject = RepositoryPaths.Relative(RepositoryPaths.SmokeTestProjectPath);
        string testProject = RepositoryPaths.Relative(RepositoryPaths.TestProjectPath);
        IReadOnlySet<string> xunit = XunitPackages.ToHashSet(StringComparer.Ordinal);
        IReadOnlySet<string> smoke = ReferencedPackages(RepositoryPaths.SmokeTestProjectPath);
        IReadOnlySet<string> test = ReferencedPackages(RepositoryPaths.TestProjectPath);

        FailureReport report = new($"Docs/{DocumentName} and {smokeProject} disagree about packages");

        string prose = Whitespace.Replace(
            string.Join(' ', MarkdownDocument.LoadDoc(DocumentName).Section(PackagesHeading).ProseLines
                .Select(line => line.Text)),
            " ");
        if (!prose.Contains(SmokeProjectClaim, StringComparison.Ordinal))
        {
            report.Add($"the document no longer says \"{SmokeProjectClaim}\" — update this test to match it");
        }

        CrossCheck.ReportMissing(
            report,
            smoke.Order(StringComparer.Ordinal),
            xunit,
            package => $"{package} is referenced by {smokeProject}, which takes the xUnit packages only");

        CrossCheck.ReportMissing(
            report,
            XunitPackages,
            smoke,
            package => $"{package} is not referenced by {smokeProject}");

        CrossCheck.ReportMissing(
            report,
            XunitPackages,
            test,
            package => $"{package} is not referenced by {testProject}, so the two projects no longer share it");

        report.AssertEmpty();
    }

    private static void AssertPackagesMatch(string projectPath)
    {
        string project = RepositoryPaths.Relative(projectPath);
        IReadOnlySet<string> referenced = ReferencedPackages(projectPath);
        IReadOnlySet<string> documented = DocumentedPackages(project);

        FailureReport report = new($"Docs/{DocumentName} and {project} disagree about packages");

        CrossCheck.ReportMissing(
            report,
            referenced.Order(StringComparer.Ordinal),
            documented,
            package => $"{package} is referenced by {project} but has no table row");

        CrossCheck.ReportMissing(
            report,
            documented.Order(StringComparer.Ordinal),
            referenced,
            package => $"{package} has a table row but {project} does not reference it");

        report.AssertEmpty();
    }

    /// <summary>
    /// Read as XML rather than matched as text: attribute order is free, and a commented-out reference
    /// is not a reference. A reference without <c>Include</c> throws instead of being skipped.
    /// </summary>
    private static IReadOnlySet<string> ReferencedPackages(string projectPath) =>
        XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value
                ?? throw new InvalidOperationException(
                    $"{RepositoryPaths.Relative(projectPath)}: a PackageReference without Include — " +
                    "this test cannot tell what it references."))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// The package table introduced by the paragraph naming this .csproj. The document has one per
    /// project with packages of its own, and they are told apart by the file the paragraph above
    /// mentions — the same way a reader does it. A paragraph that names a .csproj without a table under
    /// it (the one about the smoke test project) introduces nothing and contributes nothing.
    /// </summary>
    private static IReadOnlySet<string> DocumentedPackages(string project)
    {
        MarkdownSection section = MarkdownDocument.LoadDoc(DocumentName).Section(PackagesHeading);

        MarkdownTable table = section.Tables.FirstOrDefault(
                candidate => string.Equals(IntroducedBy(section, candidate), project, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Docs/{DocumentName}: no package table is introduced for {project}. A table belongs to " +
                $"the project the paragraph above it names.");

        return table.CodeSpanColumn(PackageColumn).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The .csproj named by the last prose line above a table. Reading upwards rather than tracking
    /// state through the section keeps the two tables independent of each other.
    /// </summary>
    private static string? IntroducedBy(MarkdownSection section, MarkdownTable table) =>
        section.ProseLines
            .Where(line => line.Number < table.Line)
            .Reverse()
            .SelectMany(line => MarkdownDocument.CodeSpans(line.Text))
            .FirstOrDefault(span => span.EndsWith(ProjectExtension, StringComparison.Ordinal));
}

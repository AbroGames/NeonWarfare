using System.Text.RegularExpressions;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Docs/Stack.md lists what the two projects depend on and what each dependency is for. Adding a
/// package is one line in a .csproj and nothing asks for the other half — the table is the only place
/// that says why the package is there at all.
/// </summary>
public class StackDocTests
{
    private static readonly Regex PackageReferenceRegex =
        new(@"<PackageReference\s+Include=""(?<name>[^""]+)""", RegexOptions.Compiled);

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

    private static IReadOnlySet<string> ReferencedPackages(string projectPath) =>
        PackageReferenceRegex.Matches(File.ReadAllText(projectPath))
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// The package table introduced by the paragraph naming this .csproj. The document has two of them,
    /// one per project, and they are told apart by the file the paragraph above mentions — the same way
    /// a reader does it. A paragraph that names a .csproj without a table under it (the one about the
    /// smoke test project) introduces nothing and contributes nothing.
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

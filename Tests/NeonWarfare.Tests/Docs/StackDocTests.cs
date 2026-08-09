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

    private const string PackagesHeading = "Stack and dependencies";

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
        IReadOnlySet<string> documented = DocumentedPackages(projectPath);

        FailureReport report = new($"Docs/Stack.md and {project} disagree about packages");

        foreach (string package in referenced.Order(StringComparer.Ordinal))
        {
            if (!documented.Contains(package))
            {
                report.Add($"{package} is referenced by {project} but has no table row");
            }
        }

        foreach (string package in documented.Order(StringComparer.Ordinal))
        {
            if (!referenced.Contains(package))
            {
                report.Add($"{package} has a table row but {project} does not reference it");
            }
        }

        report.AssertEmpty();
    }

    private static IReadOnlySet<string> ReferencedPackages(string projectPath) =>
        PackageReferenceRegex.Matches(File.ReadAllText(projectPath))
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// The package table that follows the paragraph naming this .csproj. The document has two of them,
    /// one per project, and they are told apart by the file the paragraph above mentions — the same way
    /// a reader does it. A paragraph that names a .csproj without a table under it (the one explaining
    /// <c>Compile Remove</c>) introduces nothing and contributes nothing.
    /// </summary>
    private static IReadOnlySet<string> DocumentedPackages(string projectPath)
    {
        string project = RepositoryPaths.Relative(projectPath);
        Dictionary<string, HashSet<string>> byProject = new(StringComparer.Ordinal);
        string? owner = null;
        bool insideTable = false;

        foreach (MarkdownLine line in Document.Section(PackagesHeading))
        {
            if (line.IsTableRow)
            {
                insideTable = true;
                if (owner is not null)
                {
                    // The first cell holds the package, the second the reason it is there. The header
                    // row has no code span and drops out on its own.
                    string firstCell =
                        line.Text.Split('|', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                        ?? string.Empty;

                    if (!byProject.TryGetValue(owner, out HashSet<string>? packages))
                    {
                        packages = new HashSet<string>(StringComparer.Ordinal);
                        byProject[owner] = packages;
                    }

                    packages.UnionWith(MarkdownDocument.CodeSpans(firstCell));
                }

                continue;
            }

            if (insideTable)
            {
                insideTable = false;
                owner = null;
            }

            string? mentioned = MarkdownDocument.CodeSpans(line.Text)
                .FirstOrDefault(span => span.EndsWith(".csproj", StringComparison.Ordinal));
            if (mentioned is not null)
            {
                owner = mentioned;
            }
        }

        Assert.True(
            byProject.ContainsKey(project),
            $"Docs/Stack.md: no package table is introduced for {project}");

        return byProject[project];
    }

    private static MarkdownDocument Document =>
        MarkdownDocument.Load(Path.Combine(RepositoryPaths.DocsDirectory, "Stack.md"));
}

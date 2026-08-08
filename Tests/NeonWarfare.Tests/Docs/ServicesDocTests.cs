using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Docs/Services.md is the map of both service registries. Neither of them is enumerable from the code
/// at a glance — the global one is a list of static fields, the world one is a folder of classes — so
/// the tables are what a reader actually goes by, and nothing makes them follow a rename.
/// </summary>
public class ServicesDocTests
{
    private const string GlobalServicesHeading = "Global services";

    private const string WorldServicesHeading = "World services";

    private const string ServicesClass = "Services";

    private const string ServicesPrefix = "Services.";

    private const string ServiceSuffix = "Service";

    /// <summary>
    /// Not a world service despite living in the folder: it is not a child node of World, is not
    /// registered anywhere, and is constructed by the AI pathfinding through a constructor with
    /// arguments (see Pathfinder.cs). A row in the world services table would say something untrue.
    /// </summary>
    private static readonly string[] NotWorldServices = ["NavigationService"];

    [Fact]
    public void DocumentedGlobalServices_ExistInServicesClass()
    {
        IReadOnlyDictionary<string, string> declared = DeclaredGlobalServices();
        FailureReport report = new($"Docs/Services.md rows that {RepositoryPaths.Relative(RepositoryPaths.ServicesPath)} does not back");

        foreach ((string field, string type) in DocumentedGlobalServices())
        {
            if (!declared.TryGetValue(field, out string? declaredType))
            {
                report.Add($"Services.{field} — no such member");
                continue;
            }

            if (!string.Equals(declaredType, type, StringComparison.Ordinal))
            {
                report.Add($"Services.{field} — the table says {type}, the code says {declaredType}");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void GlobalServices_AreDocumented()
    {
        IReadOnlySet<string> documented = DocumentedGlobalServices()
            .Select(row => row.Field)
            .ToHashSet(StringComparer.Ordinal);

        // The KludgeBox services are named in the paragraph above the table instead of getting a row
        // each — the document says so, and that counts as documented.
        IReadOnlySet<string> mentioned = MentionedBeforeTable(GlobalServicesHeading);

        FailureReport report = new("Members of Services that Docs/Services.md never mentions");

        foreach ((string field, _) in DeclaredGlobalServices())
        {
            if (!documented.Contains(field) && !mentioned.Contains(field))
            {
                report.Add($"Services.{field} — add a table row or name it in the paragraph above");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void WorldServiceClasses_AreDocumented()
    {
        IReadOnlySet<string> documented = DocumentedWorldServices();
        FailureReport report = new("World service classes missing from the Docs/Services.md table");

        foreach (string service in DeclaredWorldServices().Order(StringComparer.Ordinal))
        {
            if (!documented.Contains(service) && !NotWorldServices.Contains(service))
            {
                report.Add($"{service} — add a row, or list it in NotWorldServices with the reason");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void DocumentedWorldServices_ExistAsClasses()
    {
        IReadOnlySet<string> declared = DeclaredWorldServices();
        FailureReport report = new("Docs/Services.md names world services that no class backs");

        foreach (string service in DocumentedWorldServices().Order(StringComparer.Ordinal))
        {
            // Only the names shaped like a world service: the Purpose cells also mention Godot types
            // such as MultiplayerSpawner, which are nobody's service.
            if (service.StartsWith("World", StringComparison.Ordinal)
                && service.EndsWith(ServiceSuffix, StringComparison.Ordinal)
                && !declared.Contains(service))
            {
                report.Add($"{service} — renamed or deleted, the row is stale");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The static members of the <c>Services</c> class itself, by name. The nested <c>Global</c> class is
    /// not one of them: it re-exposes two of these for the global usings and adds nothing.
    /// </summary>
    private static IReadOnlyDictionary<string, string> DeclaredGlobalServices()
    {
        CSharpFile file = CSharpFile.Load(RepositoryPaths.ServicesPath);
        ClassDeclarationSyntax registry = file.Nodes<ClassDeclarationSyntax>()
            .First(declaration => declaration.Identifier.ValueText == ServicesClass);

        Dictionary<string, string> members = new(StringComparer.Ordinal);

        foreach (MemberDeclarationSyntax member in registry.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                    {
                        members[variable.Identifier.ValueText] = field.Declaration.Type.ToString();
                    }

                    break;

                case PropertyDeclarationSyntax property:
                    members[property.Identifier.ValueText] = property.Type.ToString();
                    break;
            }
        }

        return members;
    }

    /// <summary>The rows of the global services table: the member name and the class it is declared as.</summary>
    private static IEnumerable<(string Field, string Type)> DocumentedGlobalServices()
    {
        foreach (MarkdownLine line in Document.Section(GlobalServicesHeading).Where(line => line.IsTableRow))
        {
            IReadOnlyList<string> cells = Cells(line.Text);
            if (cells.Count < 2)
            {
                continue;
            }

            string? field = MarkdownDocument.CodeSpans(cells[0])
                .FirstOrDefault(span => span.StartsWith(ServicesPrefix, StringComparison.Ordinal));
            string? type = MarkdownDocument.CodeSpans(cells[1]).FirstOrDefault();

            if (field is not null && type is not null)
            {
                yield return (field[ServicesPrefix.Length..], type);
            }
        }
    }

    /// <summary>Everything written in backticks in the world services table, cells included.</summary>
    private static IReadOnlySet<string> DocumentedWorldServices() =>
        Document.Section(WorldServicesHeading)
            .Where(line => line.IsTableRow)
            .SelectMany(line => MarkdownDocument.CodeSpans(line.Text))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>The classes under Scenes/World/Service whose name marks them as a service.</summary>
    private static IReadOnlySet<string> DeclaredWorldServices() =>
        RepositoryPaths.WorldServiceFiles()
            .Select(CSharpFile.Load)
            .SelectMany(file => file.Nodes<ClassDeclarationSyntax>())
            .Select(declaration => declaration.Identifier.ValueText)
            .Where(name => name.EndsWith(ServiceSuffix, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Code spans of the prose that comes before the table of a section.</summary>
    private static IReadOnlySet<string> MentionedBeforeTable(string heading)
    {
        HashSet<string> mentioned = new(StringComparer.Ordinal);

        foreach (MarkdownLine line in Document.Section(heading))
        {
            if (line.IsTableRow)
            {
                break;
            }

            mentioned.UnionWith(MarkdownDocument.CodeSpans(line.Text));
        }

        return mentioned;
    }

    /// <summary>The cells of a table row, without the empty edges the outer pipes produce.</summary>
    private static IReadOnlyList<string> Cells(string row) =>
        row.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

    private static MarkdownDocument Document =>
        MarkdownDocument.Load(Path.Combine(RepositoryPaths.DocsDirectory, "Services.md"));
}

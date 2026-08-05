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
    private const string DocumentName = "Services.md";

    private const string GlobalServicesHeading = "Global services";

    private const string WorldServicesHeading = "World services";

    private const string ServicesClass = "Services";

    private const string ServicesPrefix = "Services.";

    private const string ServiceSuffix = "Service";

    private const int FieldColumn = 0;

    private const int ClassColumn = 1;

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
        FailureReport report = new(
            $"Docs/{DocumentName} rows that {RepositoryPaths.Relative(RepositoryPaths.ServicesPath)} does not back");

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

        FailureReport report = new($"Members of Services that Docs/{DocumentName} never mentions");

        CrossCheck.ReportMissing(
            report,
            DeclaredGlobalServices().Keys,
            documented,
            mentioned,
            field => $"Services.{field} — add a table row or name it in the paragraph above");

        report.AssertEmpty();
    }

    [Fact]
    public void WorldServiceClasses_AreDocumented()
    {
        FailureReport report = new($"World service classes missing from the Docs/{DocumentName} table");

        CrossCheck.ReportMissing(
            report,
            DeclaredWorldServices().Order(StringComparer.Ordinal),
            DocumentedWorldServices(),
            NotWorldServices,
            service => $"{service} — add a row, or list it in NotWorldServices with the reason");

        report.AssertEmpty();
    }

    [Fact]
    public void DocumentedWorldServices_ExistAsClasses()
    {
        IReadOnlySet<string> declared = DeclaredWorldServices();
        FailureReport report = new($"Docs/{DocumentName} names world services that no class backs");

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
        MarkdownTable table = Table(GlobalServicesHeading, "the global registry is gone",
            "Service", "Class", "Purpose");

        foreach (IReadOnlyList<string> row in table.Rows)
        {
            string? field = MarkdownDocument.CodeSpans(row[FieldColumn])
                .FirstOrDefault(span => span.StartsWith(ServicesPrefix, StringComparison.Ordinal));
            string? type = MarkdownTable.SingleCodeSpan(row[ClassColumn]);

            if (field is not null && type is not null)
            {
                yield return (field[ServicesPrefix.Length..], type);
            }
        }
    }

    /// <summary>Everything written in backticks in the world services table, cells included.</summary>
    private static IReadOnlySet<string> DocumentedWorldServices() =>
        Table(WorldServicesHeading, "the world registry is gone", "Service", "Purpose").Rows
            .SelectMany(row => row.SelectMany(MarkdownDocument.CodeSpans))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>The classes under Scenes/World/Service whose name marks them as a service.</summary>
    private static IReadOnlySet<string> DeclaredWorldServices() =>
        RepositoryPaths.WorldServiceFiles()
            .Select(CSharpFile.Load)
            .SelectMany(file => file.Nodes<ClassDeclarationSyntax>())
            .Select(declaration => declaration.Identifier.ValueText)
            .Where(name => name.EndsWith(ServiceSuffix, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Code spans of the prose that comes before the table of a section. What follows the table is a
    /// different subject — under "Global services" it is about the global usings — and counting it
    /// would let a service be considered documented by an unrelated mention.
    /// </summary>
    private static IReadOnlySet<string> MentionedBeforeTable(string heading)
    {
        HashSet<string> mentioned = new(StringComparer.Ordinal);

        foreach (MarkdownLine line in Document().Section(heading).Lines)
        {
            if (line.IsTableRow)
            {
                break;
            }

            mentioned.UnionWith(MarkdownDocument.CodeSpans(line.Text));
        }

        return mentioned;
    }

    private static MarkdownTable Table(string heading, string whatIsGone, params string[] columns) =>
        Document().Section(heading).RequireTable(whatIsGone, columns);

    private static MarkdownDocument Document() => MarkdownDocument.LoadDoc(DocumentName);
}

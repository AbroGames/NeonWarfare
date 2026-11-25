using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// Docs/Services.md is the map of the global service registry. It is not enumerable from the code at a
/// glance — it is a list of static fields — so the table is what a reader actually goes by, and nothing
/// makes it follow a rename.
/// </summary>
public class ServicesDocTests
{
    private const string DocumentName = "Services.md";

    private const string GlobalServicesHeading = "Global services";

    private const string ServicesClass = "Services";

    private const string ServicesPrefix = "Services.";

    private const int FieldColumn = 0;

    private const int ClassColumn = 1;

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

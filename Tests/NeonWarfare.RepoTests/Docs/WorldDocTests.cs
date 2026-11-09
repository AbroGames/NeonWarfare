using NeonWarfare.RepoTests.Architecture;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// The inventory of Docs/World.md is the map of the World machinery: the services of <c>World/Infra/</c> are found by
/// their attributes and never named together anywhere in the code, so a new type, or a renamed one, leaves the
/// document behind silently. The commands, events, notices and models are left out: they are many, they belong to
/// whoever publishes them, and their base type already says what they are.
/// </summary>
[Collection(GameAssembly.Collection)]
public class WorldDocTests
{
    private const string DocumentName = "World.md";

    private const string InventoryHeading = "Inventory";

    private const string InfraNamespace = WorldLayers.WorldNamespace + ".Infra";

    private const int TypeColumn = 0;

    private const int PurposeColumn = 1;

    private static readonly string[] Columns = ["Type", "What it is"];

    private static readonly string[] ExcludedBases =
    [
        WorldLayers.WorldNamespace + ".Infra.Protocol.Command",
        WorldLayers.WorldNamespace + ".Infra.Protocol.Event",
        WorldLayers.WorldNamespace + ".Infra.Hud.Notice",
    ];

    [Fact]
    public void WorldTypes_AreInTheInventory()
    {
        FailureReport report = new($"World root and Infra types missing from the inventory of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            DeclaredTypes().Order(StringComparer.Ordinal),
            DocumentedTypes().ToHashSet(StringComparer.Ordinal),
            type => $"{type} — add it to the table of its folder");

        report.AssertEmpty();
    }

    [Fact]
    public void InventoryRows_PointToExistingTypes()
    {
        FailureReport report = new($"Types in the inventory of Docs/{DocumentName} that the World does not declare");

        CrossCheck.ReportMissing(
            report,
            DocumentedTypes(),
            DeclaredTypes(),
            type => $"{type} — renamed, moved or deleted, the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void InventoryRows_NameTypesAndDescribeThem()
    {
        FailureReport report = new($"Malformed rows of the inventory of Docs/{DocumentName}");
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (MarkdownTable table in Tables())
        {
            DocTableChecks.CellIsNotEmpty(table, report, PurposeColumn, TypeColumn, "what the type is");
            foreach (IReadOnlyList<string> row in table.Rows)
            {
                List<string> types = MarkdownDocument.CodeSpans(row[TypeColumn]).ToList();
                if (types.Count == 0)
                {
                    report.Add($"'{row[TypeColumn]}' — the {Columns[TypeColumn]} cell must name types in backticks");
                }

                foreach (string type in types.Where(type => !seen.Add(type)))
                {
                    report.Add($"{type} — listed twice");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The top-level types of the World root namespace and of every namespace under <c>Infra</c>, by name without the
    /// generic arity: <c>IPlayerCommandHandler</c> for <c>IPlayerCommandHandler`1</c>.
    /// </summary>
    private static IReadOnlySet<string> DeclaredTypes() =>
        GameAssembly.Instance.Types
            .Where(type => !type.IsNested && !type.Name.StartsWith('<') && IsWorldRootOrInfra(type.Namespace))
            .Where(type => !WorldLayers.IsModel(type)
                           && !ExcludedBases.Any(excluded => WorldLayers.DerivesFrom(type, excluded)))
            .Select(type => WithoutArity(type.Name))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Every type named in the first column of the inventory tables, without its type parameters.</summary>
    private static IReadOnlyList<string> DocumentedTypes() =>
        Tables()
            .SelectMany(table => table.Rows)
            .SelectMany(row => MarkdownDocument.CodeSpans(row[TypeColumn]))
            .Select(span => span.Split('<')[0])
            .ToList();

    private static IReadOnlyList<MarkdownTable> Tables()
    {
        MarkdownSection inventory = MarkdownDocument.LoadDoc(DocumentName).Section(InventoryHeading);
        if (inventory.Tables.Count == 0)
        {
            throw new InvalidOperationException($"Docs/{DocumentName}: no table under '{InventoryHeading}'.");
        }

        foreach (MarkdownTable table in inventory.Tables.Where(table => !table.Header.SequenceEqual(Columns)))
        {
            throw new InvalidOperationException(
                $"{table.RelativePath}:{table.Line}: an inventory table must have the columns " +
                $"{string.Join(" | ", Columns)}, it has {string.Join(" | ", table.Header)}.");
        }

        return inventory.Tables;
    }

    private static bool IsWorldRootOrInfra(string ns) =>
        ns == WorldLayers.WorldNamespace
        || ns == InfraNamespace
        || ns.StartsWith(InfraNamespace + ".", StringComparison.Ordinal);

    private static string WithoutArity(string name) => name.Split('`')[0];
}

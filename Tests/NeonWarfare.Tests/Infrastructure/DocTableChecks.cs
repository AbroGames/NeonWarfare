namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// The shape every inventory table in <c>Docs/</c> is written in: one identifier in backticks in the
/// first column, a line of explanation in the second, one row per thing. The checks that compare such a
/// table with the code read cells by index and by code span, so a row written differently would not
/// fail — it would quietly drop out of the comparison and take its subject with it.
/// </summary>
public static class DocTableChecks
{
    /// <summary>
    /// The column holds exactly one code span per row, and no two rows hold the same one. A cell with
    /// none or several is what makes a row invisible to the rest of the checks; a duplicate is a
    /// subject documented twice, of which only one row is ever read.
    /// </summary>
    public static void SingleCodeSpanPerRow(
        MarkdownTable table, FailureReport report, int column, string what)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (IReadOnlyList<string> row in table.Rows)
        {
            string? span = MarkdownTable.SingleCodeSpan(row[column]);
            if (span is null)
            {
                report.Add($"'{row[column]}' — the {table.Header[column]} cell must hold exactly one " +
                           $"{what} in backticks");
                continue;
            }

            if (!seen.Add(span))
            {
                report.Add($"{span} — listed twice");
            }
        }
    }

    /// <summary>
    /// The column says something. An empty cell is a row that names a subject and explains nothing,
    /// which is the state the table exists to prevent.
    /// </summary>
    public static void CellIsNotEmpty(
        MarkdownTable table, FailureReport report, int column, int subjectColumn, string what)
    {
        foreach (IReadOnlyList<string> row in table.Rows)
        {
            if (row[column].Length == 0)
            {
                string subject = MarkdownTable.SingleCodeSpan(row[subjectColumn]) ?? row[subjectColumn];
                report.Add($"{subject} — the row says nothing about {what}");
            }
        }
    }
}

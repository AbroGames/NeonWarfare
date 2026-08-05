using System.Text;
using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A parsed markdown file: raw content for the formatting checks, plus links, headings and tables taken
/// from a sanitized copy where fenced blocks and inline code spans are blanked out. Sanitizing matters —
/// Docs/Code-style.md contains things like <c>[Rpc(...)]</c> inside code, and README.md has
/// <c>`[Child]`</c>; parsing those as links would produce phantom failures.
/// </summary>
public sealed class MarkdownDocument
{
    private static readonly Regex LinkRegex =
        new(@"\[(?<text>[^\]]*)\]\((?<target>[^)\s]*)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);

    private static readonly Regex HeadingRegex =
        new(@"^(?<level>#{1,6})\s+(?<text>.+?)\s*#*\s*$", RegexOptions.Compiled);

    private static readonly Regex FenceRegex =
        new(@"^\s{0,3}(?<fence>`{3,}|~{3,})", RegexOptions.Compiled);

    private static readonly Regex InlineCodeRegex =
        new(@"(?<ticks>`+)(?:[^`]|(?!\k<ticks>)`)*\k<ticks>", RegexOptions.Compiled);

    private static readonly Regex InlineLinkTextRegex =
        new(@"\[(?<text>[^\]]*)\]\([^)]*\)", RegexOptions.Compiled);

    private static readonly Regex TableDelimiterCellRegex = new(@"^:?-+:?$", RegexOptions.Compiled);

    private MarkdownDocument(string path, byte[] bytes, string text)
    {
        Path = path;
        Bytes = bytes;
        Text = text;
        Lines = TextFile.SplitLines(text);

        bool[] insideFence = MapFences(Lines);
        string[] sanitized = Sanitize(Lines, insideFence);

        Links = ParseLinks(path, sanitized);
        Headings = ParseHeadings(Lines, insideFence);
        HeadingSlugs = BuildSlugs(Headings);
        Tables = ParseTables(path, Lines, sanitized, insideFence);
        IsFenced = insideFence;
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>Raw bytes — the BOM check needs them, a decoded string would hide it.</summary>
    public byte[] Bytes { get; }

    /// <summary>Raw text, line endings untouched — what the "ends with one newline" check reads.</summary>
    public string Text { get; }

    /// <summary>Lines split on LF, each without its trailing CR.</summary>
    public string[] Lines { get; }

    /// <summary><c>IsFenced[i]</c> — line <c>i</c> is inside a fenced code block (fences included).</summary>
    public bool[] IsFenced { get; }

    public IReadOnlyList<MarkdownLink> Links { get; }

    public IReadOnlyList<MarkdownHeading> Headings { get; }

    /// <summary>GitHub-style anchors of all headings, including the <c>-1</c>, <c>-2</c> duplicates.</summary>
    public IReadOnlySet<string> HeadingSlugs { get; }

    /// <summary>Every pipe table, in file order. Cell text keeps its markup — backticks included.</summary>
    public IReadOnlyList<MarkdownTable> Tables { get; }

    public static MarkdownDocument Load(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = new UTF8Encoding(false).GetString(TextFile.StripBom(bytes));
        return new MarkdownDocument(System.IO.Path.GetFullPath(path), bytes, text);
    }

    /// <summary>A file of <c>Docs/</c> by its file name — how every doc test reaches its document.</summary>
    public static MarkdownDocument LoadDoc(string fileName) => Load(RepositoryPaths.Doc(fileName));

    /// <summary>
    /// The part of the document under a heading, down to the next heading of the same or a higher level.
    /// The checks that compare a document with the code work section by section: Docs/Services.md has one
    /// table for the global services and another for the world ones, and they mean different things.
    /// <br/>
    /// A missing heading throws rather than yielding nothing: a renamed heading has to read as "this
    /// test is looking at the wrong place", not as "the section is empty and everything checks out".
    /// </summary>
    public MarkdownSection Section(string headingText)
    {
        MarkdownHeading heading = Headings.FirstOrDefault(
                candidate => string.Equals(candidate.Text, headingText, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"{RelativePath}: no heading '{headingText}'. Either it was renamed, or the test is " +
                $"looking at the wrong document.");

        int end = Headings
            .Where(candidate => candidate.Line > heading.Line && candidate.Level <= heading.Level)
            .Select(candidate => candidate.Line)
            .DefaultIfEmpty(Lines.Length + 1)
            .First();

        List<MarkdownLine> lines = [];
        for (int number = heading.Line + 1; number < end && number <= Lines.Length; number++)
        {
            lines.Add(new MarkdownLine(number, Lines[number - 1], IsFenced[number - 1]));
        }

        IReadOnlyList<MarkdownTable> tables = Tables
            .Where(table => table.Line > heading.Line && table.Line < end)
            .ToList();

        return new MarkdownSection(this, heading, lines, tables);
    }

    /// <summary>
    /// GitHub heading anchor: markdown markup is dropped, the text is lowercased, everything except
    /// letters, digits, <c>-</c>, <c>_</c> and spaces is removed, and only then spaces become
    /// hyphens. The order matters: "Роли процесса: `IsServer` / `IsClient`" has to end up as
    /// "роли-процесса-isserver--isclient" — the double hyphen is what is left of the slash between
    /// two spaces. Letters are kept by category, so Cyrillic survives, as GitHub does it.
    /// </summary>
    public static string Slugify(string headingText)
    {
        string text = InlineLinkTextRegex.Replace(headingText, "${text}");
        text = text.Replace("`", string.Empty).Replace("*", string.Empty).Replace("~", string.Empty);
        text = text.ToLowerInvariant();

        StringBuilder kept = new(text.Length);
        foreach (char symbol in text)
        {
            if (char.IsLetterOrDigit(symbol) || symbol is '-' or '_' or ' ')
            {
                kept.Append(symbol);
            }
        }

        return kept.ToString().Trim().Replace(' ', '-');
    }

    /// <summary>
    /// The contents of every inline code span on a line. The tables that describe the code write the
    /// identifiers in backticks, which is what makes them findable without parsing markdown tables.
    /// </summary>
    public static IEnumerable<string> CodeSpans(string line) =>
        InlineCodeRegex.Matches(line).Select(match => match.Value.Trim('`').Trim());

    private static bool[] MapFences(string[] lines)
    {
        bool[] insideFence = new bool[lines.Length];
        string? openFence = null;

        for (int i = 0; i < lines.Length; i++)
        {
            Match match = FenceRegex.Match(lines[i]);
            if (openFence is null)
            {
                if (match.Success)
                {
                    openFence = match.Groups["fence"].Value;
                    insideFence[i] = true;
                }

                continue;
            }

            insideFence[i] = true;
            bool closes = match.Success
                          && match.Groups["fence"].Value[0] == openFence[0]
                          && match.Groups["fence"].Value.Length >= openFence.Length;
            if (closes)
            {
                openFence = null;
            }
        }

        return insideFence;
    }

    /// <summary>Blanks out fenced blocks and inline code, keeping line count and line lengths.</summary>
    private static string[] Sanitize(string[] lines, bool[] insideFence)
    {
        string[] sanitized = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            sanitized[i] = insideFence[i]
                ? new string(' ', lines[i].Length)
                : InlineCodeRegex.Replace(lines[i], match => new string(' ', match.Length));
        }

        return sanitized;
    }

    private static List<MarkdownLink> ParseLinks(string path, string[] sanitized)
    {
        List<MarkdownLink> links = [];
        for (int i = 0; i < sanitized.Length; i++)
        {
            foreach (Match match in LinkRegex.Matches(sanitized[i]))
            {
                links.Add(new MarkdownLink(
                    path,
                    i + 1,
                    match.Groups["text"].Value,
                    match.Groups["target"].Value));
            }
        }

        return links;
    }

    private static List<MarkdownHeading> ParseHeadings(string[] lines, bool[] insideFence)
    {
        List<MarkdownHeading> headings = [];
        for (int i = 0; i < lines.Length; i++)
        {
            if (insideFence[i])
            {
                continue;
            }

            Match match = HeadingRegex.Match(lines[i]);
            if (match.Success)
            {
                headings.Add(new MarkdownHeading(
                    match.Groups["level"].Value.Length,
                    match.Groups["text"].Value,
                    i + 1));
            }
        }

        return headings;
    }

    /// <summary>
    /// Pipe tables. A table starts where a line with a pipe is followed by a delimiter row, and ends on
    /// the first line without one. The rows are read from <paramref name="lines"/> but split at the
    /// pipes of <paramref name="sanitized"/>, so a pipe inside an inline code span stays inside its
    /// cell — and the cell keeps the backticks, which in these tables are the content itself.
    /// </summary>
    private static List<MarkdownTable> ParseTables(
        string path, string[] lines, string[] sanitized, bool[] insideFence)
    {
        List<MarkdownTable> tables = [];

        for (int i = 0; i + 1 < lines.Length; i++)
        {
            bool startsTable = !insideFence[i]
                               && sanitized[i].Contains('|')
                               && !insideFence[i + 1]
                               && IsDelimiterRow(sanitized[i + 1]);
            if (!startsTable)
            {
                continue;
            }

            IReadOnlyList<string> header = SplitCells(lines, sanitized, i);
            List<IReadOnlyList<string>> rows = [];

            int row = i + 2;
            for (; row < lines.Length && !insideFence[row] && sanitized[row].Contains('|'); row++)
            {
                IReadOnlyList<string> cells = SplitCells(lines, sanitized, row);
                if (cells.Count != header.Count)
                {
                    throw new InvalidOperationException(
                        $"{RepositoryPaths.Relative(path)}:{row + 1}: the table row has {cells.Count} " +
                        $"cell(s), its header has {header.Count}: '{lines[row]}'.");
                }

                rows.Add(cells);
            }

            tables.Add(new MarkdownTable(path, i + 1, header, rows));
            i = row - 1;
        }

        return tables;
    }

    /// <summary>A row of nothing but <c>---</c> cells, with the optional alignment colons.</summary>
    private static bool IsDelimiterRow(string sanitized)
    {
        if (!sanitized.Contains('|'))
        {
            return false;
        }

        List<string> cells = DropOuterEmpties(sanitized.Split('|').Select(cell => cell.Trim()).ToList());
        return cells.Count > 0 && cells.All(cell => TableDelimiterCellRegex.IsMatch(cell));
    }

    /// <summary>
    /// Cells of one row. The text comes from <paramref name="lines"/> and keeps its markup; the split
    /// points come from <paramref name="sanitized"/>, where inline code is blanked out, so a pipe
    /// inside a code span never ends a cell. A pipe escaped with a backslash does not end one either —
    /// Docs/Chat-and-commands.md writes <c>{add\|remove}</c> — and is left in the cell as written.
    /// </summary>
    private static List<string> SplitCells(string[] lines, string[] sanitized, int index)
    {
        string raw = lines[index];
        List<string> cells = [];
        int start = 0;

        for (int i = 0; i < sanitized[index].Length; i++)
        {
            if (sanitized[index][i] != '|' || (i > 0 && raw[i - 1] == '\\'))
            {
                continue;
            }

            cells.Add(raw[start..i].Trim());
            start = i + 1;
        }

        cells.Add(raw[start..].Trim());
        return DropOuterEmpties(cells);
    }

    /// <summary>
    /// The rows here are written with an outer pipe on both sides, which leaves an empty cell at each
    /// end. GFM allows the outer pipes to be omitted, so the cells are dropped only when they are there.
    /// </summary>
    private static List<string> DropOuterEmpties(List<string> cells)
    {
        if (cells.Count > 0 && cells[0].Length == 0)
        {
            cells.RemoveAt(0);
        }

        if (cells.Count > 0 && cells[^1].Length == 0)
        {
            cells.RemoveAt(cells.Count - 1);
        }

        return cells;
    }

    private static HashSet<string> BuildSlugs(IReadOnlyList<MarkdownHeading> headings)
    {
        HashSet<string> slugs = [];
        Dictionary<string, int> seen = [];

        foreach (MarkdownHeading heading in headings)
        {
            string slug = Slugify(heading.Text);
            int occurrence = seen.TryGetValue(slug, out int previous) ? previous + 1 : 0;
            seen[slug] = occurrence;
            slugs.Add(occurrence == 0 ? slug : $"{slug}-{occurrence}");
        }

        return slugs;
    }
}

/// <summary>One ATX heading: its level, its raw text and its one-based line number.</summary>
public sealed record MarkdownHeading(int Level, string Text, int Line);

/// <summary>
/// One line of a document, with the one-based number an editor shows and whether it sits inside a
/// fenced block — a fenced line is an example, not a statement, and most checks skip it.
/// </summary>
public sealed record MarkdownLine(int Number, string Text, bool IsFenced)
{
    /// <summary>True for a line of a markdown table, header and separator included.</summary>
    public bool IsTableRow => Text.TrimStart().StartsWith('|');
}

/// <summary>
/// Everything written under one heading: the lines and the tables. This is the unit the checks that
/// compare a document with the code work in — a document holds several independent lists, and a check
/// that read the whole file would compare the code against the wrong one.
/// </summary>
public sealed class MarkdownSection
{
    private readonly MarkdownDocument _document;

    internal MarkdownSection(
        MarkdownDocument document,
        MarkdownHeading heading,
        IReadOnlyList<MarkdownLine> lines,
        IReadOnlyList<MarkdownTable> tables)
    {
        _document = document;
        Heading = heading;
        Lines = lines;
        Tables = tables;
    }

    public MarkdownHeading Heading { get; }

    /// <summary>The lines under the heading, the heading itself excluded, fenced blocks included.</summary>
    public IReadOnlyList<MarkdownLine> Lines { get; }

    /// <summary>Every table of the section, in document order. Docs/Stack.md has two under one heading.</summary>
    public IReadOnlyList<MarkdownTable> Tables { get; }

    /// <summary>The lines that are prose: no fenced blocks, no table rows.</summary>
    public IEnumerable<MarkdownLine> ProseLines =>
        Lines.Where(line => !line.IsFenced && !line.IsTableRow);

    /// <summary>
    /// The one table of the section, with its columns checked before anything reads a cell by index.
    /// A dropped or reordered column has to say so, not surface as an index out of range three checks
    /// later, and a missing table has to name what is gone rather than throw a null reference.
    /// </summary>
    public MarkdownTable RequireTable(string whatIsGone, params string[] columns)
    {
        MarkdownTable table = Tables.FirstOrDefault() ?? throw new InvalidOperationException(
            $"{_document.RelativePath}: no table under '{Heading.Text}'. Either the heading was " +
            $"renamed, or {whatIsGone}.");

        if (!table.Header.SequenceEqual(columns, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{_document.RelativePath}:{table.Line}: the table under '{Heading.Text}' must have the " +
                $"columns {string.Join(" | ", columns)}, it has {string.Join(" | ", table.Header)}.");
        }

        return table;
    }
}

/// <summary>
/// One pipe table: the one-based line of its header, the header cells and the body rows. Every row has
/// as many cells as the header — <see cref="MarkdownDocument"/> refuses to read a ragged one.
/// </summary>
public sealed record MarkdownTable(
    string Path,
    int Line,
    IReadOnlyList<string> Header,
    IReadOnlyList<IReadOnlyList<string>> Rows)
{
    /// <summary>Repository-relative path of the document the table is in, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>
    /// The single code span of a cell, or <c>null</c> when the cell holds none or several. These tables
    /// write every identifier in backticks; a cell that does not is reported by
    /// <see cref="DocTableChecks.SingleCodeSpanPerRow"/> rather than guessed at here.
    /// </summary>
    public static string? SingleCodeSpan(string cell)
    {
        List<string> spans = MarkdownDocument.CodeSpans(cell).ToList();
        return spans.Count == 1 ? spans[0] : null;
    }

    /// <summary>The single code span of one column, row by row, cells without one left out.</summary>
    public IEnumerable<string> CodeSpanColumn(int column) =>
        Rows.Select(row => SingleCodeSpan(row[column])).OfType<string>();
}

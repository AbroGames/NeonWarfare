using System.Text;
using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A parsed markdown file: raw content for the formatting checks, plus links and headings taken from
/// a sanitized copy where fenced blocks and inline code spans are blanked out. Sanitizing matters —
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

    private const string RaggedRowMessage =
        "{Path}:{Line}: the table row has {Actual} cell(s), its header has {Expected}: '{Text}'.";

    private MarkdownDocument(string path, byte[] bytes, string text)
    {
        Path = path;
        Bytes = bytes;
        Text = text;
        Lines = text.Split('\n');

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

    /// <summary>Raw bytes — the BOM check needs them, a decoded string would hide it.</summary>
    public byte[] Bytes { get; }

    /// <summary>Raw text, line endings untouched.</summary>
    public string Text { get; }

    /// <summary>Lines split on LF; a trailing CR, if any, is left in place for the format checks.</summary>
    public string[] Lines { get; }

    /// <summary><c>IsFenced[i]</c> — line <c>i</c> is inside a fenced code block (fences included).</summary>
    public bool[] IsFenced { get; }

    public IReadOnlyList<MarkdownLink> Links { get; }

    public IReadOnlyList<MarkdownHeading> Headings { get; }

    /// <summary>GitHub-style anchors of all headings, including the <c>-1</c>, <c>-2</c> duplicates.</summary>
    public IReadOnlySet<string> HeadingSlugs { get; }

    /// <summary>Every pipe table, in file order. Cell text keeps its markup — backticks included.</summary>
    public IReadOnlyList<MarkdownTable> Tables { get; }

    /// <summary>
    /// The first table that follows the given heading, or null when the section has none. Looking the
    /// table up by its heading rather than by its index keeps a check working when another table is
    /// added elsewhere in the document.
    /// </summary>
    public MarkdownTable? TableUnder(string headingText)
    {
        (int start, int end) = SectionRange(headingText);
        return Tables.FirstOrDefault(table => table.Line > start && table.Line < end);
    }

    /// <summary>
    /// The raw lines of the section under the given heading, the heading itself and fenced blocks
    /// excluded. Empty when there is no such heading.
    /// </summary>
    public IEnumerable<string> LinesUnder(string headingText)
    {
        (int start, int end) = SectionRange(headingText);
        for (int line = start + 1; line < end; line++)
        {
            if (!IsFenced[line - 1])
            {
                yield return Lines[line - 1].TrimEnd('\r');
            }
        }
    }

    /// <summary>
    /// The one-based line of the heading and of the first heading after it that is not nested inside
    /// it — the half-open range the section occupies. <c>(0, 0)</c> when the heading is absent.
    /// </summary>
    private (int Start, int End) SectionRange(string headingText)
    {
        MarkdownHeading? heading = Headings.FirstOrDefault(
            candidate => string.Equals(candidate.Text, headingText, StringComparison.Ordinal));
        if (heading is null)
        {
            return (0, 0);
        }

        int end = Headings
            .Where(candidate => candidate.Line > heading.Line && candidate.Level <= heading.Level)
            .Select(candidate => candidate.Line)
            .DefaultIfEmpty(Lines.Length + 1)
            .First();

        return (heading.Line, end);
    }

    public static MarkdownDocument Load(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = new UTF8Encoding(false).GetString(StripBom(bytes));
        return new MarkdownDocument(System.IO.Path.GetFullPath(path), bytes, text);
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
    /// The lines under a heading, up to the next heading of the same or a higher level. The checks that
    /// compare a document with the code work section by section: Docs/Services.md has one table for the
    /// global services and another for the world ones, and they mean different things.
    /// </summary>
    public IReadOnlyList<MarkdownLine> Section(string headingText)
    {
        MarkdownHeading? heading = Headings.FirstOrDefault(candidate =>
            string.Equals(candidate.Text, headingText, StringComparison.Ordinal));

        if (heading is null)
        {
            throw new InvalidOperationException(
                $"{RepositoryPaths.Relative(Path)}: no heading '{headingText}'. Either it was renamed, " +
                $"or the test is looking at the wrong document.");
        }

        MarkdownHeading? next = Headings
            .Where(candidate => candidate.Line > heading.Line && candidate.Level <= heading.Level)
            .MinBy(candidate => candidate.Line);

        int first = heading.Line;
        int last = next?.Line - 1 ?? Lines.Length;

        return Enumerable.Range(first, last - first + 1)
            .Where(number => number <= Lines.Length)
            .Select(number => new MarkdownLine(number, Lines[number - 1].TrimEnd('\r')))
            .ToList();
    }

    /// <summary>
    /// The contents of every inline code span on a line. The tables that describe the code write the
    /// identifiers in backticks, which is what makes them findable without parsing markdown tables.
    /// </summary>
    public static IEnumerable<string> CodeSpans(string line) =>
        InlineCodeRegex.Matches(line).Select(match => match.Value.Trim('`').Trim());

    private static byte[] StripBom(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? bytes[3..]
            : bytes;

    private static bool[] MapFences(string[] lines)
    {
        bool[] insideFence = new bool[lines.Length];
        string? openFence = null;

        for (int i = 0; i < lines.Length; i++)
        {
            Match match = FenceRegex.Match(lines[i].TrimEnd('\r'));
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
            string line = lines[i].TrimEnd('\r');
            sanitized[i] = insideFence[i]
                ? new string(' ', line.Length)
                : InlineCodeRegex.Replace(line, match => new string(' ', match.Length));
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

            Match match = HeadingRegex.Match(lines[i].TrimEnd('\r'));
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
                    throw new InvalidOperationException(RaggedRowMessage
                        .Replace("{Path}", RepositoryPaths.Relative(path))
                        .Replace("{Line}", (row + 1).ToString())
                        .Replace("{Actual}", cells.Count.ToString())
                        .Replace("{Expected}", header.Count.ToString())
                        .Replace("{Text}", lines[row].TrimEnd('\r')));
                }

                rows.Add(cells);
            }

            tables.Add(new MarkdownTable(i + 1, header, rows));
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
        string raw = lines[index].TrimEnd('\r');
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

/// <summary>One line of a document, with the one-based number an editor shows.</summary>
public sealed record MarkdownLine(int Number, string Text)
{
    /// <summary>True for a line of a markdown table, header and separator included.</summary>
    public bool IsTableRow => Text.TrimStart().StartsWith('|');
}

/// <summary>
/// One pipe table: the one-based line of its header, the header cells and the body rows. Every row has
/// as many cells as the header — <see cref="MarkdownDocument"/> refuses to read a ragged one.
/// </summary>
public sealed record MarkdownTable(
    int Line,
    IReadOnlyList<string> Header,
    IReadOnlyList<IReadOnlyList<string>> Rows);

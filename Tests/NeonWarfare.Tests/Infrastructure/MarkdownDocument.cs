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

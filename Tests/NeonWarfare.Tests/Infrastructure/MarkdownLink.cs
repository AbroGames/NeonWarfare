namespace NeonWarfare.Tests.Infrastructure;

/// <summary>One inline markdown link — <c>[text](target)</c> — with where it was found.</summary>
public sealed class MarkdownLink
{
    private static readonly string[] ExternalSchemes = ["http://", "https://", "mailto:"];

    public MarkdownLink(string sourceFile, int line, string text, string target)
    {
        SourceFile = sourceFile;
        Line = line;
        Text = text;
        Target = target;
    }

    /// <summary>Absolute path of the file the link was found in.</summary>
    public string SourceFile { get; }

    /// <summary>One-based line number.</summary>
    public int Line { get; }

    public string Text { get; }

    public string Target { get; }

    public bool IsExternal =>
        ExternalSchemes.Any(scheme => Target.StartsWith(scheme, StringComparison.OrdinalIgnoreCase));

    /// <summary>True for links to a section of the very same file — <c>[text](#section)</c>.</summary>
    public bool IsAnchorOnly => Target.StartsWith('#');

    /// <summary>The part before <c>#</c>, percent-decoded. Empty for anchor-only links.</summary>
    public string PathPart
    {
        get
        {
            int hash = Target.IndexOf('#');
            string path = hash < 0 ? Target : Target[..hash];
            return Uri.UnescapeDataString(path);
        }
    }

    /// <summary>The part after <c>#</c>, percent-decoded. Empty when the link has no anchor.</summary>
    public string Anchor
    {
        get
        {
            int hash = Target.IndexOf('#');
            return hash < 0 ? string.Empty : Uri.UnescapeDataString(Target[(hash + 1)..]);
        }
    }

    public string Describe() => $"{RepositoryPaths.Relative(SourceFile)}:{Line} → {Target}";
}

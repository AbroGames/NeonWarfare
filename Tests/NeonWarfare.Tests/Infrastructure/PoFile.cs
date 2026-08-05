using System.Text;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A gettext file — <c>Assets/Locales/*.po</c> or <c>messages.pot</c> — read as a list of key and
/// translation pairs. Only the subset of the format the project actually uses is supported; anything
/// else throws instead of being skipped, so the tests can never quietly check a file they misread.
/// </summary>
public sealed class PoFile
{
    private PoFile(string path, IReadOnlyList<PoEntry> entries)
    {
        Path = path;
        Entries = entries;
        Keys = entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>Every entry in file order, header excluded. Duplicate keys are kept as-is.</summary>
    public IReadOnlyList<PoEntry> Entries { get; }

    public IReadOnlySet<string> Keys { get; }

    /// <summary>True for <c>messages.pot</c>, whose translations are meant to be empty.</summary>
    public bool IsTemplate =>
        System.IO.Path.GetExtension(Path).Equals(".pot", StringComparison.OrdinalIgnoreCase);

    public static PoFile Load(string path)
    {
        string absolutePath = System.IO.Path.GetFullPath(path);
        string[] lines = TextFile.ReadLines(absolutePath);
        List<PoEntry> entries = [];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            // The header entry is a msgid "" whose msgstr is continued over several quoted lines.
            // Those continuations are the only ones the project has, and they carry no key.
            if (line.StartsWith('"'))
            {
                continue;
            }

            if (line.StartsWith("msgstr ", StringComparison.Ordinal))
            {
                continue;
            }

            if (!line.StartsWith("msgid ", StringComparison.Ordinal))
            {
                throw Unsupported(absolutePath, i + 1, line);
            }

            string key = Unquote(line["msgid ".Length..], absolutePath, i + 1);
            if (key.Length == 0)
            {
                continue;
            }

            string? next = i + 1 < lines.Length ? lines[i + 1] : null;
            if (next is null || !next.StartsWith("msgstr ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{RepositoryPaths.Relative(absolutePath)}:{i + 1}: msgid '{key}' is not followed " +
                    $"by a msgstr line.");
            }

            // A real key must fit one line. A continuation here would mean the translation is longer
            // than what Entries reports, and Translations_AreNotEmpty would be checking a fragment.
            string? after = i + 2 < lines.Length ? lines[i + 2] : null;
            if (after is not null && after.StartsWith('"'))
            {
                throw Unsupported(absolutePath, i + 3, after);
            }

            entries.Add(new PoEntry(
                key,
                Unquote(next["msgstr ".Length..], absolutePath, i + 2),
                i + 1));
            i++;
        }

        return new PoFile(absolutePath, entries);
    }

    /// <summary>All three localization files: en.po, ru.po and the template.</summary>
    public static IReadOnlyList<PoFile> LoadAll() =>
        RepositoryPaths.LocaleFilesAndTemplate().Select(Load).ToList();

    /// <summary>
    /// Strips the surrounding quotes and undoes the escapes gettext writes. The project's own strings
    /// contain none, but the header does, and a wrong value here would be silent.
    /// </summary>
    private static string Unquote(string quoted, string path, int line)
    {
        string trimmed = quoted.Trim();
        if (trimmed.Length < 2 || !trimmed.StartsWith('"') || !trimmed.EndsWith('"'))
        {
            throw Unsupported(path, line, quoted);
        }

        string body = trimmed[1..^1];
        StringBuilder result = new(body.Length);
        for (int i = 0; i < body.Length; i++)
        {
            if (body[i] != '\\' || i + 1 == body.Length)
            {
                result.Append(body[i]);
                continue;
            }

            i++;
            result.Append(body[i] switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => body[i],
            });
        }

        return result.ToString();
    }

    /// <summary>
    /// The one refusal this parser has. Only the subset of gettext the project uses is supported, and
    /// anything else throws instead of being skipped — a skipped line is a key the tests never see.
    /// </summary>
    private static InvalidOperationException Unsupported(string path, int line, string text) =>
        new($"{RepositoryPaths.Relative(path)}:{line}: PoFile does not support this line: '{text}'. " +
            $"The project keeps one key per single-line msgid/msgstr pair — see Docs/Localization.md. " +
            $"Extend the parser before using the extended format.");
}

/// <summary>One localization key, its translation and the one-based line the key is on.</summary>
public sealed record PoEntry(string Key, string Translation, int Line);

using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Localization;

/// <summary>
/// The rules Docs/Localization.md states about Assets/Locales: a new key goes into all three files,
/// keys are SCREAMING_SNAKE_CASE grouped by screen, and messages.pot stays a template. Nothing here is
/// visible to the compiler — a key added to en.po only shows up as a raw key on the Russian screen.
/// </summary>
public class LocaleFilesTests
{
    [Fact]
    public void LocaleFiles_HaveTheSameKeySet()
    {
        IReadOnlyList<PoFile> files = PoFile.LoadAll();
        PoFile template = files.Single(file => file.IsTemplate);
        FailureReport report = new(
            $"Locale files disagree with {template.RelativePath} on which keys exist");

        foreach (PoFile file in files.Where(file => !file.IsTemplate))
        {
            foreach (string missing in template.Keys.Except(file.Keys).Order(StringComparer.Ordinal))
            {
                report.Add($"{file.RelativePath}: key '{missing}' is missing");
            }

            foreach (string extra in file.Keys.Except(template.Keys).Order(StringComparer.Ordinal))
            {
                report.Add($"{file.RelativePath}: key '{extra}' is absent from the template");
            }
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(FileSources.Locales), MemberType = typeof(FileSources))]
    public void LocaleFile_HasNoDuplicateKeys(string relativePath)
    {
        PoFile file = PoFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: duplicate keys");

        foreach (IGrouping<string, PoEntry> duplicate in file.Entries
                     .GroupBy(entry => entry.Key, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            string lines = string.Join(", ", duplicate.Select(entry => entry.Line));
            report.Add($"'{duplicate.Key}' appears {duplicate.Count()} times, on lines {lines}");
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// messages.pot decides the order, and every locale repeats it. The grouping by screen only helps if
    /// the same key sits in the same place in all the files: that is what makes two locales diffable side
    /// by side, and what keeps a new key from being appended to the end of one file and into its group in
    /// another. Keys the file and the template do not share are left to LocaleFiles_HaveTheSameKeySet.
    /// </summary>
    [Theory]
    [MemberData(nameof(FileSources.Translations), MemberType = typeof(FileSources))]
    public void LocaleKeys_AreInTheSameOrderAsInTheTemplate(string relativePath)
    {
        PoFile template = PoFile.Load(RepositoryPaths.LocaleTemplatePath);
        PoFile file = PoFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new(
            $"{relativePath}: keys are not in the {template.RelativePath} order");

        List<string> expected = template.Entries
            .Select(entry => entry.Key)
            .Where(file.Keys.Contains)
            .ToList();
        List<PoEntry> actual = file.Entries
            .Where(entry => template.Keys.Contains(entry.Key))
            .ToList();

        for (int i = 0; i < Math.Min(expected.Count, actual.Count); i++)
        {
            if (!string.Equals(expected[i], actual[i].Key, StringComparison.Ordinal))
            {
                report.Add(
                    $"line {actual[i].Line}: position {i + 1} holds '{actual[i].Key}', " +
                    $"but the template has '{expected[i]}' there");
            }
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(FileSources.Locales), MemberType = typeof(FileSources))]
    public void LocaleKeys_FollowNamingConvention(string relativePath)
    {
        PoFile file = PoFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new(
            $"{relativePath}: keys must be SCREAMING_SNAKE_CASE grouped by screen with '__'");

        foreach (PoEntry entry in file.Entries.Where(entry => !LocalizationKeys.IsKey(entry.Key)))
        {
            report.Add($"line {entry.Line}: '{entry.Key}'");
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(FileSources.Translations), MemberType = typeof(FileSources))]
    public void Translations_AreNotEmpty(string relativePath)
    {
        PoFile file = PoFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: keys left untranslated");

        foreach (PoEntry entry in file.Entries.Where(entry => entry.Translation.Length == 0))
        {
            report.Add($"line {entry.Line}: '{entry.Key}' has an empty msgstr");
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// messages.pot is the template every locale is filled in from. A translation that leaks into it
    /// turns one language into the default for the others, so its msgstr must stay empty.
    /// </summary>
    [Fact]
    public void Template_HasEmptyTranslations()
    {
        PoFile template = PoFile.Load(RepositoryPaths.LocaleTemplatePath);
        FailureReport report = new($"{template.RelativePath}: the template must not carry translations");

        foreach (PoEntry entry in template.Entries.Where(entry => entry.Translation.Length > 0))
        {
            report.Add($"line {entry.Line}: '{entry.Key}' has msgstr '{entry.Translation}'");
        }

        report.AssertEmpty();
    }
}

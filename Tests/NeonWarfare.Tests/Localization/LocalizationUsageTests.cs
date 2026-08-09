using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Localization;

/// <summary>
/// Ties Assets/Locales to the places the keys are used. Both directions matter and neither is caught by
/// the build: a key used but not translated shows up as raw SCREAMING_SNAKE text on the screen, and a
/// key nobody uses stays in all three files forever, asking to be translated again on every pass.
/// </summary>
public class LocalizationUsageTests
{
    [Fact]
    public void UsedKeys_ExistInLocaleFiles()
    {
        HashSet<string> known = PoFile.Load(RepositoryPaths.LocaleTemplatePath).Keys
            .ToHashSet(StringComparer.Ordinal);
        FailureReport report = new("Localization keys used in the game but absent from Assets/Locales");

        foreach (LocalizationKeyUsage usage in LocalizationKeys.Usages()
                     .Where(usage => !known.Contains(usage.Key))
                     .OrderBy(usage => usage.RelativePath, StringComparer.Ordinal)
                     .ThenBy(usage => usage.Line))
        {
            report.Add($"{usage.Describe()}: '{usage.Key}' is in no locale file");
        }

        report.AssertEmpty();
    }

    [Fact]
    public void LocaleKeys_AreUsedInCodeOrScenes()
    {
        PoFile template = PoFile.Load(RepositoryPaths.LocaleTemplatePath);
        HashSet<string> used = LocalizationKeys.Usages()
            .Select(usage => usage.Key)
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new(
            "Localization keys nobody uses — remove them from en.po, ru.po and messages.pot");

        foreach (PoEntry entry in template.Entries.Where(entry => !used.Contains(entry.Key)))
        {
            report.Add($"{template.RelativePath}:{entry.Line}: '{entry.Key}'");
        }

        report.AssertEmpty();
    }
}

using System.Text.RegularExpressions;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Launch;

/// <summary>
/// A Multi-Launch configuration in .run/ names the launch profiles it starts as plain strings
/// ("runConfig:.NET Launch Settings Profile.NeonWarfare: Server"). Renaming a profile in
/// Properties/launchSettings.json therefore breaks the configuration without a word from anyone —
/// Rider only complains at the moment someone presses Run — and leaves the Multi-Launch list of
/// Docs/Quick-start.md describing something that no longer exists.
/// </summary>
public class MultiLaunchTests
{
    private const string DocumentName = "Quick-start.md";

    private const string MultiLaunchHeading = "Multi-Launch: server and clients with one button";

    /// <summary>
    /// A list item as the document writes it:
    /// <c>* Type: `Multi-Launch`. Name: `Fast-test (1 client)`. Tasks: `Server, Autoconnect (1)`.</c>
    /// Read from the raw text rather than through MarkdownDocument: that class blanks out code spans
    /// on purpose, and here the names are exactly what is inside them.
    /// </summary>
    private static readonly Regex DocumentedConfigRegex = new(
        @"^\*\s+Type:\s+`Multi-Launch`\.\s+Name:\s+`(?<name>[^`]+)`\.\s+Tasks:\s+`(?<tasks>[^`]+)`\.\s*$",
        RegexOptions.Compiled);

    private const string TaskSeparator = ", ";

    [Fact]
    public void RunConfigs_AreDocumented()
    {
        IReadOnlyDictionary<string, RunConfigFile> existing = ExistingConfigs();
        FailureReport report = new(
            $"Multi-Launch configurations in {RepositoryPaths.Relative(RepositoryPaths.RunConfigsDirectory)} " +
            $"missing from the '{MultiLaunchHeading}' list of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            existing.Keys,
            DocumentedConfigs().Select(config => config.Name).ToHashSet(StringComparer.Ordinal),
            name => $"{existing[name].RelativePath}: '{name}' — add a list item for it");

        report.AssertEmpty();
    }

    [Fact]
    public void DocumentedRunConfigs_Exist()
    {
        FailureReport report = new(
            $"Multi-Launch configurations described in Docs/{DocumentName} that " +
            $"{RepositoryPaths.Relative(RepositoryPaths.RunConfigsDirectory)} does not contain");

        CrossCheck.ReportMissing(
            report,
            DocumentedConfigs().Select(config => config.Name),
            ExistingConfigs().Keys.ToHashSet(StringComparer.Ordinal),
            name => $"'{name}' — either the file was renamed, or the list item is stale");

        report.AssertEmpty();
    }

    /// <summary>
    /// The tasks start in the order the file lists them — the server first, then the clients — so the
    /// document has to keep that order too, not merely the same set of names.
    /// </summary>
    [Fact]
    public void DocumentedTasks_MatchRunConfigs()
    {
        Dictionary<string, IReadOnlyList<string>> documented = new(StringComparer.Ordinal);
        foreach (DocumentedConfig config in DocumentedConfigs())
        {
            documented[config.Name] = config.Tasks;
        }

        FailureReport report = new(
            $"Task lists in the '{MultiLaunchHeading}' section of Docs/{DocumentName} that disagree " +
            "with the .run/ files");

        foreach (RunConfigFile config in RunConfigFile.LoadAll())
        {
            if (!documented.TryGetValue(config.Name, out IReadOnlyList<string>? tasks))
            {
                continue;
            }

            if (!tasks.SequenceEqual(config.ProfileNames, StringComparer.Ordinal))
            {
                report.Add(
                    $"{config.RelativePath}: the document says '{string.Join(TaskSeparator, tasks)}', " +
                    $"the file starts '{string.Join(TaskSeparator, config.ProfileNames)}'");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The check that answers whether the configuration runs at all: a task pointing at a profile that
    /// no longer exists is the failure mode the whole file is exposed to.
    /// </summary>
    [Fact]
    public void RunConfigTasks_ExistInLaunchSettings()
    {
        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        HashSet<string> profiles = settings.ProfileNames.ToHashSet(StringComparer.Ordinal);
        FailureReport report = new(
            $"Multi-Launch tasks referring to a profile that {settings.RelativePath} does not declare");

        foreach (RunConfigFile config in RunConfigFile.LoadAll())
        {
            foreach (string profile in config.ProfileNames.Where(profile => !profiles.Contains(profile)))
            {
                report.Add($"{config.RelativePath}: '{profile}'");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// Rider does not require the two to agree, but when they disagree the run widget shows one name
    /// while the repository and the document speak about another.
    /// </summary>
    [Theory]
    [MemberData(nameof(FileSources.RunConfigs), MemberType = typeof(FileSources))]
    public void RunConfigName_MatchesFileName(string relativePath)
    {
        RunConfigFile config = RunConfigFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: the configuration name does not match the file name");

        if (!string.Equals(config.Name, config.FileName, StringComparison.Ordinal))
        {
            report.Add($"the file is named '{config.FileName}', the configuration '{config.Name}'");
        }

        report.AssertEmpty();
    }

    /// <summary>The list items of the Multi-Launch section, in document order.</summary>
    private static IReadOnlyList<DocumentedConfig> DocumentedConfigs()
    {
        List<DocumentedConfig> configs = [];

        foreach (MarkdownLine line in MarkdownDocument.LoadDoc(DocumentName)
                     .Section(MultiLaunchHeading).ProseLines)
        {
            Match match = DocumentedConfigRegex.Match(line.Text);
            if (match.Success)
            {
                configs.Add(new DocumentedConfig(
                    match.Groups["name"].Value,
                    match.Groups["tasks"].Value.Split(TaskSeparator)));
            }
        }

        if (configs.Count == 0)
        {
            throw new InvalidOperationException(
                $"Docs/{DocumentName} has no Multi-Launch list items under '## {MultiLaunchHeading}'. " +
                "The .run/ checks have nothing to compare against.");
        }

        return configs;
    }

    /// <summary>Every configuration in .run/, by the name Rider shows for it.</summary>
    private static IReadOnlyDictionary<string, RunConfigFile> ExistingConfigs() =>
        RunConfigFile.LoadAll().ToDictionary(config => config.Name, StringComparer.Ordinal);

    /// <summary>One list item: the configuration name and the profiles it says are started.</summary>
    private sealed record DocumentedConfig(string Name, IReadOnlyList<string> Tasks);
}

using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Launch;

/// <summary>
/// The "Run profiles" table of Docs/Quick-start.md is the only description the launch profiles have:
/// a profile is a JSON object with a free-form name, so renaming one, adding one or changing its
/// arguments says nothing to the compiler and leaves the document quietly wrong. The document itself
/// asks for the two to be edited in sync — this is that request made into a check.
/// </summary>
public class LaunchProfilesTests
{
    private const string DocumentName = "Quick-start.md";

    private const string RunProfilesHeading = "Run profiles";

    /// <summary>The Arguments cell of a profile that adds nothing to the project path.</summary>
    private const string NoArgumentsCell = "—";

    private const int ProfileColumn = 0;

    private const int ArgumentsColumn = 1;

    /// <summary>
    /// Profiles that do not start the game. <c>All tests</c> runs <c>dotnet test</c> over the solution — a
    /// shortcut for the developer, not a way to launch the game, so it has no project path to pass and no
    /// place in a table of game launch modes.
    /// </summary>
    private static readonly string[] NotGameProfiles = ["All tests"];

    [Fact]
    public void Profiles_AreDocumented()
    {
        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        FailureReport report = new(
            $"Profiles declared in {settings.RelativePath} but missing from the '{RunProfilesHeading}' " +
            $"table of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            GameProfiles(settings).Select(profile => profile.Name),
            DocumentedProfiles().Select(profile => profile.Name).ToHashSet(StringComparer.Ordinal),
            name => $"'{name}' — add a table row for it");

        report.AssertEmpty();
    }

    [Fact]
    public void DocumentedProfiles_Exist()
    {
        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        FailureReport report = new(
            $"Profiles described in the '{RunProfilesHeading}' table of Docs/{DocumentName} that " +
            $"{settings.RelativePath} does not declare");

        CrossCheck.ReportMissing(
            report,
            DocumentedProfiles().Select(profile => profile.Name),
            GameProfiles(settings).Select(profile => profile.Name).ToHashSet(StringComparer.Ordinal),
            name => $"'{name}' — either it was renamed in launchSettings.json, or the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void DocumentedArguments_MatchLaunchSettings()
    {
        Dictionary<string, string> documented = new(StringComparer.Ordinal);
        foreach (DocumentedProfile profile in DocumentedProfiles())
        {
            documented[profile.Name] = profile.ArgumentsCell;
        }

        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        FailureReport report = new(
            $"Arguments in the '{RunProfilesHeading}' table of Docs/{DocumentName} that disagree with " +
            settings.RelativePath);

        foreach (LaunchProfile profile in GameProfiles(settings))
        {
            if (!documented.TryGetValue(profile.Name, out string? cell))
            {
                continue;
            }

            string expected = profile.ArgumentsWithoutProjectPath;
            string actual = MarkdownTable.SingleCodeSpan(cell) ?? string.Empty;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                report.Add(
                    $"'{profile.Name}': the table says '{cell}', launchSettings.json says " +
                    $"'{Describe(expected)}'");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The table is read top to bottom next to the file, so a row that stays in place while the profile
    /// moves is just as misleading as a missing one.
    /// </summary>
    [Fact]
    public void TableOrder_MatchesLaunchSettings()
    {
        IReadOnlyList<DocumentedProfile> documented = DocumentedProfiles();
        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        IReadOnlyList<string> declared = GameProfiles(settings).Select(profile => profile.Name).ToList();
        FailureReport report = new(
            $"The '{RunProfilesHeading}' table of Docs/{DocumentName} lists the profiles in an order " +
            $"other than {settings.RelativePath}");

        for (int i = 0; i < Math.Min(documented.Count, declared.Count); i++)
        {
            if (!string.Equals(documented[i].Name, declared[i], StringComparison.Ordinal))
            {
                report.Add(
                    $"row {i + 1}: the table has '{documented[i].Name}', the file has '{declared[i]}'");
            }
        }

        // The loop above stops at the shorter of the two. Which names the extra rows hold is reported
        // by the two checks above; that there are extra rows at all is only visible here.
        if (documented.Count != declared.Count)
        {
            report.Add($"the table has {documented.Count} row(s), {settings.RelativePath} declares " +
                       $"{declared.Count} profile(s)");
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// Godot needs the project folder to start at all, and the table leaves that argument out of every
    /// row on purpose. A profile without it would both fail to run and make its Arguments cell mean
    /// something other than it does in the rows around it.
    /// </summary>
    [Fact]
    public void Profiles_PassProjectPath()
    {
        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        FailureReport report = new(
            $"Profiles in {settings.RelativePath} that do not start with " +
            $"'{LaunchSettingsFile.ProjectPathArgument}'");

        foreach (LaunchProfile profile in GameProfiles(settings).Where(profile => !profile.HasProjectPath))
        {
            report.Add($"'{profile.Name}': '{profile.CommandLineArgs}'");
        }

        report.AssertEmpty();
    }

    [Fact]
    public void NotGameProfiles_AreDeclared()
    {
        LaunchSettingsFile settings = LaunchSettingsFile.Load();
        CrossCheck.AssertExemptionsExist(
            nameof(NotGameProfiles),
            NotGameProfiles,
            settings.ProfileNames.Contains,
            $"{settings.RelativePath} declares no such profile");
    }

    /// <summary>Every profile that starts the game, in file order.</summary>
    private static IEnumerable<LaunchProfile> GameProfiles(LaunchSettingsFile settings) =>
        settings.Profiles.Where(profile => !NotGameProfiles.Contains(profile.Name));

    /// <summary>The table rows in document order. A duplicated row is kept — the order check reports it.</summary>
    private static IReadOnlyList<DocumentedProfile> DocumentedProfiles() =>
        MarkdownDocument.LoadDoc(DocumentName)
            .Section(RunProfilesHeading)
            .RequireTable(
                "the launch profile checks have nothing to compare against",
                "Profile", "Arguments", "What it does")
            .Rows
            .Select(row => new DocumentedProfile(
                MarkdownTable.SingleCodeSpan(row[ProfileColumn]) ?? row[ProfileColumn],
                row[ArgumentsColumn]))
            .ToList();

    /// <summary>An empty argument list still has to be shown as something in a failure message.</summary>
    private static string Describe(string arguments) =>
        arguments.Length == 0 ? NoArgumentsCell : arguments;

    /// <summary>One row of the table: the profile name and the Arguments cell exactly as written.</summary>
    private sealed record DocumentedProfile(string Name, string ArgumentsCell);
}

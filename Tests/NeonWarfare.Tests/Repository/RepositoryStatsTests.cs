using System.Text;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Repository;

/// <summary>
/// The size of the repository, printed and never asserted on. Every other test states a rule and fails
/// when it is broken; this one states nothing — the numbers move on every commit, and a threshold on
/// them would only be noise to silence. It is here because the question "how much is there by now"
/// comes up regularly and is otherwise answered by an ad-hoc shell one-liner that nobody keeps.
/// The output is visible with
/// <c>dotnet test --filter FullyQualifiedName~RepositoryStatsTests -l "console;verbosity=detailed"</c>.
/// </summary>
public class RepositoryStatsTests
{
    private readonly ITestOutputHelper _output;

    public RepositoryStatsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Statistics_ArePrintedForInformation()
    {
        IReadOnlyList<string> allFiles = RepositoryPaths.AllFiles();
        StringBuilder report = new();

        report.AppendLine("Repository statistics");
        AppendTextDirectory(report, "Docs/", RepositoryPaths.DocsDirectory);
        AppendTextDirectory(report, "brain/", RepositoryPaths.BrainDirectory);
        AppendCode(report, "*.cs (game)", RepositoryPaths.SourceFiles());
        AppendCode(
            report,
            "*.cs (tests)",
            RepositoryPaths.TestFiles().Concat(RepositoryPaths.SmokeTestFiles()).ToList());
        AppendCode(report, "*.tscn", FilesWithExtension(allFiles, ".tscn"));
        report.AppendLine($"  messages.pot: {TemplateKeyCount()} keys");
        report.AppendLine($"  Assets/: {RepositoryPaths.AllFilesUnder(RepositoryPaths.AssetsDirectory).Count} files");
        report.AppendLine($"  whole project: {allFiles.Count} files");

        _output.WriteLine(report.ToString().TrimEnd());
    }

    /// <summary>
    /// Files, lines and words of a directory of prose. An absent directory is reported as such rather
    /// than as zeroes: brain/ is not checked in, and "0 files" would read like it is empty.
    /// </summary>
    private static void AppendTextDirectory(StringBuilder report, string label, string directory)
    {
        if (!Directory.Exists(directory))
        {
            report.AppendLine($"  {label}: absent");
            return;
        }

        IReadOnlyList<string> files = RepositoryPaths.AllFilesUnder(directory);
        int lines = 0;
        int words = 0;

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            lines += TextFile.LineCount(text);
            words += text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        report.AppendLine($"  {label}: {files.Count} files, {lines} lines, {words} words");
    }

    /// <summary>
    /// Files and lines of one group of sources. The .cs of the repository are split by project rather
    /// than counted together: Scenes/ and Scripts/ are the game, Tests/ is the suite checking it, and a
    /// single number hides which of the two is growing.
    /// </summary>
    private static void AppendCode(StringBuilder report, string label, IReadOnlyList<string> files)
    {
        int lines = files.Sum(file => TextFile.LineCount(File.ReadAllText(file)));

        report.AppendLine($"  {label}: {files.Count} files, {lines} lines");
    }

    private static IReadOnlyList<string> FilesWithExtension(IEnumerable<string> files, string extension) =>
        files
            .Where(path => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static int TemplateKeyCount() =>
        PoFile.Load(RepositoryPaths.LocaleTemplatePath).Keys.Count;
}

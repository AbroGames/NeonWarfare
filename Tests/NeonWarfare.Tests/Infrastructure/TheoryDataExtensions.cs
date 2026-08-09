using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>Turns a list of repository files into the theory cases the file-by-file checks run on.</summary>
public static class TheoryDataExtensions
{
    /// <summary>
    /// One case per file, named by its repository-relative path — that name is what the test report
    /// shows, so a failure points at the file without opening the message.
    /// </summary>
    public static TheoryData<string> AsTheoryData(this IEnumerable<string> absolutePaths)
    {
        TheoryData<string> data = [];
        foreach (string path in absolutePaths)
        {
            data.Add(RepositoryPaths.Relative(path));
        }

        return data;
    }
}

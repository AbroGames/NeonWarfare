using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// MemberData sources for the run configuration tests. Repository-relative paths are used so that a
/// failing test is named after the file it failed on.
/// </summary>
public static class LaunchFileSources
{
    /// <summary>Every Multi-Launch configuration in .run/.</summary>
    public static TheoryData<string> RunConfigs => RepositoryPaths.RunConfigFiles().AsTheoryData();
}

using System.Reflection;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// The repository root, used as the working directory of every launched game process.
/// It comes from the RepositoryRoot assembly metadata baked in by the .csproj, so it does not depend
/// on how deep the build output directory happens to be.
/// </summary>
public static class RepositoryRoot
{
    private const string MetadataKey = "RepositoryRoot";

    public static string Path { get; } = ReadRoot();

    private static string ReadRoot()
    {
        string? value = typeof(RepositoryRoot).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == MetadataKey)
            ?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Assembly metadata '{MetadataKey}' is missing. It is set by an AssemblyMetadata item in " +
                "NeonWarfare.SmokeTests.csproj — the tests cannot locate the game project without it.");
        }

        return System.IO.Path.GetFullPath(value);
    }
}

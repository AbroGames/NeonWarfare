using System.Reflection;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// Locations inside the repository the tests read from.
/// The root comes from the RepositoryRoot assembly metadata baked in by the .csproj, so it does not
/// depend on how deep the build output directory happens to be.
/// </summary>
public static class RepositoryPaths
{
    private const string RepositoryRootMetadataKey = "RepositoryRoot";

    private const string MissingMetadataMessage =
        "Assembly metadata '{Key}' is missing. It is set by an AssemblyMetadata item in " +
        "NeonWarfare.Tests.csproj — tests cannot locate the repository without it.";

    public static string Root { get; } = ReadRoot();

    public static string ReadmePath { get; } = Path.Combine(Root, "README.md");

    public static string DocsDirectory { get; } = Path.Combine(Root, "Docs");

    /// <summary>All documentation files, sorted, as absolute paths.</summary>
    public static IReadOnlyList<string> DocFiles() =>
        Directory.GetFiles(DocsDirectory, "*.md", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    /// <summary>Documentation files plus README.md — everything the doc tests scan.</summary>
    public static IReadOnlyList<string> DocFilesAndReadme() =>
        DocFiles().Prepend(Path.GetFullPath(ReadmePath)).ToList();

    /// <summary>Repository-relative path with forward slashes, for readable failure messages.</summary>
    public static string Relative(string absolutePath) =>
        Path.GetRelativePath(Root, absolutePath).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>Resolves a path that a failure message reported back to an absolute one.</summary>
    public static string Absolute(string relativePath) =>
        Path.GetFullPath(Path.Combine(Root, relativePath));

    private static string ReadRoot()
    {
        string? value = typeof(RepositoryPaths).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == RepositoryRootMetadataKey)
            ?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                MissingMetadataMessage.Replace("{Key}", RepositoryRootMetadataKey));
        }

        return Path.GetFullPath(value);
    }
}

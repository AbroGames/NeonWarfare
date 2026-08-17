using Microsoft.CodeAnalysis;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// The assemblies the test project itself was compiled against, as metadata for a Roslyn compilation:
/// the net10.0 reference assemblies, GodotSharp, KludgeBox and everything they pull in transitively.
/// <br/>
/// The list is written by the WriteCompileReferences target of NeonWarfare.Tests.csproj from
/// <c>@(ReferencePath)</c> — exactly what csc received after NuGet and conflict resolution — so it is
/// never rebuilt here by guessing where the targeting pack or the NuGet cache live. Nothing on it is
/// loaded into the test process: Roslyn reads the files as metadata only.
/// </summary>
public static class CompileReferences
{
    /// <summary>The file name the csproj target writes next to the test assembly.</summary>
    private const string ListFileName = "compile-references.txt";

    private static readonly Lazy<IReadOnlyList<string>> Paths = new(ReadPaths);

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(
        () => Paths.Value.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList());

    /// <summary>Absolute paths of every compile-time reference, in the order MSBuild resolved them.</summary>
    public static IReadOnlyList<string> All() => Paths.Value;

    /// <summary>The same references, opened for a <c>CSharpCompilation</c>. Opened once per test run.</summary>
    public static IReadOnlyList<MetadataReference> AsMetadata() => References.Value;

    private static IReadOnlyList<string> ReadPaths()
    {
        string listPath = Path.Combine(AppContext.BaseDirectory, ListFileName);
        if (!File.Exists(listPath))
        {
            throw new InvalidOperationException(
                $"{listPath} is missing. It is written by the WriteCompileReferences target of " +
                $"NeonWarfare.Tests.csproj on every build — rebuild the test project.");
        }

        List<string> paths = File.ReadAllLines(listPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        // A reference that moved away (a cleared NuGet cache) would otherwise surface as a pile of
        // unrelated "type not found" errors in the compilation that uses it.
        List<string> missing = paths.Where(path => !File.Exists(path)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"{ListFileName} names files that do not exist — restore and rebuild the test project:\n" +
                string.Join("\n", missing));
        }

        return paths;
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// GodotBox is the game-independent layer over KludgeBox: it may use the engine and KludgeBox, never the
/// game. The two are built into one assembly, so the game build cannot tell — a name GodotBox takes from
/// the game resolves there just as well, and the global usings of Src/NeonWarfare/Scripts/GlobalUsings.cs
/// reach into GodotBox without a single <c>using</c> in its files.
/// <br/>
/// A syntax check would have to know every name those global usings bring in. A compilation does not:
/// GodotBox is compiled on its own, against the framework, GodotSharp and KludgeBox only, and whatever
/// it still takes from the game is a compile error. Nothing is emitted or executed; the engine is never
/// started — the references are read as metadata.
/// </summary>
public class GodotBoxIndependenceTests
{
    private const string AssemblyName = "GodotBox";

    /// <summary>
    /// The game project sets neither <c>Nullable</c> nor <c>AllowUnsafeBlocks</c>, and net10.0 fixes the
    /// language at C# 14 (<c>dotnet msbuild NeonWarfare.csproj -getProperty:LangVersion</c>). Pinned
    /// rather than Latest: a newer Roslyn in the test project must not accept what the game compiler
    /// rejects.
    /// </summary>
    private static readonly CSharpParseOptions ParseOptions = new(
        LanguageVersion.CSharp14,
        preprocessorSymbols: PreprocessorSymbols);

    /// <summary>
    /// The part of the game's <c>DefineConstants</c> in a plain <c>dotnet build</c> (Debug) that is not
    /// tied to a platform or to the exact engine version. GodotBox has no <c>#if</c> today; these are
    /// here so that one guarded by <c>GODOT</c> or <c>TOOLS</c> is compiled the way the game compiles
    /// it. The platform (<c>GODOT_LINUXBSD</c>, ...) and version (<c>GODOT4_7_1</c>, ...) constants are
    /// left out on purpose: they depend on the machine and go stale on every engine bump.
    /// </summary>
    private static string[] PreprocessorSymbols => ["GODOT", "GODOT4", "TRACE", "DEBUG", "TOOLS"];

    private static readonly CSharpCompilationOptions CompilationOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        nullableContextOptions: NullableContextOptions.Disable,
        allowUnsafe: false);

    /// <summary>
    /// Every compile error of GodotBox built without the game, one line per error, sorted and with
    /// duplicates dropped. The Godot source generators are not run: GodotBox uses nothing they generate
    /// (no <c>[Signal]</c> events, no <c>SignalName</c> / <c>MethodName</c> / <c>PropertyName</c>), and a
    /// partial class with a single part compiles as it is. Should that change, the missing member shows up
    /// here as an error and the generators have to be added to the compilation.
    /// </summary>
    [Fact]
    public void GodotBox_CompilesWithoutTheGame()
    {
        IReadOnlyList<string> files = RepositoryPaths.GodotBoxFiles();
        Assert.NotEmpty(files);

        CSharpCompilation compilation = CSharpCompilation.Create(
            AssemblyName,
            files.Select(Parse),
            CompileReferences.AsMetadata(),
            CompilationOptions);

        FailureReport report = new(
            $"Src/GodotBox does not compile without Src/NeonWarfare — it depends on the game " +
            $"({files.Count} files compiled against the framework, GodotSharp and KludgeBox only)");

        IEnumerable<string> errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(Describe)
            .DistinctBy(error => error.Text, StringComparer.Ordinal)
            .OrderBy(error => error.Path, StringComparer.Ordinal)
            .ThenBy(error => error.Line)
            .ThenBy(error => error.Text, StringComparer.Ordinal)
            .Select(error => error.Text);

        foreach (string error in errors)
        {
            report.Add(error);
        }

        report.AssertEmpty();
    }

    private static SyntaxTree Parse(string path) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(path), ParseOptions, path);

    /// <summary>
    /// <c>Src/GodotBox/Foo.cs:12: CS0103 The name 'Di' does not exist in the current context</c>. An error
    /// without a place in the source — a broken reference — is reported by its id and message alone.
    /// </summary>
    private static (string Path, int Line, string Text) Describe(Diagnostic diagnostic)
    {
        string message = $"{diagnostic.Id} {diagnostic.GetMessage()}";
        if (!diagnostic.Location.IsInSource)
        {
            return (string.Empty, 0, message);
        }

        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        string path = RepositoryPaths.Relative(span.Path);
        int line = span.StartLinePosition.Line + 1;
        return (path, line, $"{path}:{line}: {message}");
    }
}

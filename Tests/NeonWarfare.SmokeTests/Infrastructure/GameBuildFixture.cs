using System.Diagnostics;
using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;

[assembly: AssemblyFixture(typeof(GameBuildFixture))]

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Builds the game and imports its assets once before any scenario runs.
///
/// Godot launches assemblies that are already compiled, and this project deliberately has no
/// ProjectReference to NeonWarfare.csproj, so `dotnet test` on the smoke tests alone would happily
/// run them against a stale — or missing — game build. The import is done here because the scenarios run in
/// parallel, and several processes importing into .godot/ at once would race.
/// </summary>
public sealed class GameBuildFixture
{
    private const int BuildTimeoutMs = 300_000;

    public GameBuildFixture()
    {
        Run("dotnet", ["build", Path.Combine(RepositoryRoot.Path, "NeonWarfare.csproj")]);
        Run(GodotExecutable.Path, ["--headless", "--path", RepositoryRoot.Path, "--import"]);
    }

    private static void Run(string fileName, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            WorkingDirectory = RepositoryRoot.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        string command = $"{fileName} {string.Join(' ', arguments)}";
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start `{command}`.");

        // Read before waiting: a full pipe would deadlock the build.
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(BuildTimeoutMs))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"`{command}` did not finish within {BuildTimeoutMs} ms.");
        }

        if (process.ExitCode == 0) return;

        throw new InvalidOperationException(
            $"`{command}` failed with exit code {process.ExitCode}:" +
            $"{Environment.NewLine}{output.Result}{Environment.NewLine}{error.Result}");
    }
}

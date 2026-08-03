using System.Diagnostics;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Builds the game once before any scenario runs.
///
/// Godot launches assemblies that are already compiled, and this project deliberately has no
/// ProjectReference to NeonWarfare.csproj, so `dotnet test` on the smoke tests alone would happily
/// run them against a stale — or missing — game build.
/// </summary>
public sealed class GameBuildFixture
{
    private const int BuildTimeoutMs = 300_000;

    public GameBuildFixture()
    {
        string projectPath = Path.Combine(RepositoryRoot.Path, "NeonWarfare.csproj");

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = RepositoryRoot.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectPath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start `dotnet build` for the game project.");

        // Read before waiting: a full pipe would deadlock the build.
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(BuildTimeoutMs))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"`dotnet build` did not finish within {BuildTimeoutMs} ms.");
        }

        if (process.ExitCode == 0) return;

        throw new InvalidOperationException(
            $"`dotnet build {projectPath}` failed with exit code {process.ExitCode}:" +
            $"{Environment.NewLine}{output.Result}{Environment.NewLine}{error.Result}");
    }
}

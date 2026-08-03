namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// The Godot binary the smoke tests launch. There is no exported game binary in the repository, so
/// the engine itself is run against the project directory — the same way Docs/Quick-start.md and the
/// launchSettings.json profiles do it.
/// </summary>
public static class GodotExecutable
{
    private const string EnvironmentVariable = "GODOT_EXE";

    /// <summary>
    /// Absolute path to the engine binary. Throws with an actionable message when the environment is
    /// not set up — a missing variable is a machine configuration problem, not a failing assertion,
    /// and it must not read as "the game is broken".
    /// </summary>
    public static string Path
    {
        get
        {
            string? value = Environment.GetEnvironmentVariable(EnvironmentVariable);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"The {EnvironmentVariable} environment variable is not set, so the smoke tests cannot " +
                    "launch the game. It must point at the Godot .NET executable — see Docs/Quick-start.md.");
            }

            if (!File.Exists(value))
            {
                throw new InvalidOperationException(
                    $"{EnvironmentVariable} points at '{value}', but there is no file there.");
            }

            return System.IO.Path.GetFullPath(value);
        }
    }
}

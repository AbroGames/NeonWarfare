namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// The user:// directories of the processes of one scenario, so the developer's own settings and saves
/// neither affect the result nor get polluted by it, and processes do not share settings, saves or godot.log.
/// A process gets the directory of its <see cref="GameLaunch.Name"/>: the launches of one name in several
/// runs over the same instance share it, which is how a restart finds the save of the previous run.
/// Every directory is deleted on <see cref="Dispose"/>, so it must come after the processes are stopped.
/// </summary>
public sealed class UserDataDirectories : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "NeonWarfare.SmokeTests", Guid.NewGuid().ToString("N"));

    public string Of(string name)
    {
        string path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Left behind in the temp directory — harmless, and not worth failing a test over.
        }
        catch (UnauthorizedAccessException)
        {
            // Same as above.
        }
    }
}

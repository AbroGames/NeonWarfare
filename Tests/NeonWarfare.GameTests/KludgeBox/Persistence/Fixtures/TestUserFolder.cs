using KludgeBox.Persistence;

namespace NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;

/// <summary>
/// A fresh folder under <c>user://</c> of the test project, removed with everything in it by <see cref="Dispose"/>.
/// Needs the engine, which maps <c>user://</c>.
/// </summary>
public sealed class TestUserFolder : IDisposable
{
    private readonly string _name = $"test-{Guid.NewGuid():N}";
    private readonly string _path;

    public TestUserFolder()
    {
        _path = new UserStorage().GetPathForFile(_name);
        Directory.CreateDirectory(_path);
    }

    /// <summary>The name of a file in this folder, relative to <c>user://</c>.</summary>
    public string FileName(string name) => $"{_name}/{name}";

    public string FullPath(string name) => Path.Combine(_path, name);

    public string Read(string name) => File.ReadAllText(FullPath(name));

    public void Write(string name, string text) => File.WriteAllText(FullPath(name), text);

    /// <summary>The names of every file and folder in this folder, in order.</summary>
    public string[] Entries() => Directory.EnumerateFileSystemEntries(_path).Select(Path.GetFileName).Order()
        .ToArray()!;

    public void Dispose() => Directory.Delete(_path, recursive: true);
}

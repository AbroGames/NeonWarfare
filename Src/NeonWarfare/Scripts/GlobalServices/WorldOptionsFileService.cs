using KludgeBox.Logging;
using KludgeBox.Persistence;
using NeonWarfare.Scenes.Worlds.Features.Options;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices;

public class WorldOptionsFileService
{
    private readonly ILogger _log = LogFactory.GetForStatic<WorldOptionsFileService>();

    /// <summary>
    /// An option that is missing or bad in the file gets its default, written back, and the other options stay.
    /// An option out of its range is bad too.
    /// </summary>
    /// <param name="fileName"><c>null</c> — the default options, and no file is read.</param>
    public WorldOptions Read(string fileName)
    {
        if (fileName == null) return WorldOptions.GetDefault();

        var file = new JsonFile<WorldOptions>(fileName);
        WorldOptions read = file.ReadOverDefault(WorldOptions.GetDefault);
        WorldOptions sanitized = read.Sanitized();
        if (sanitized != read)
        {
            _log.Warning("Bad world options in '{fileName}' replaced: {options} with {newOptions}",
                fileName, read, sanitized);
            file.Write(sanitized);
        }
        return sanitized;
    }

    /// <returns><c>false</c> when the file could not be written.</returns>
    public bool Write(string fileName, WorldOptions options)
    {
        return new JsonFile<WorldOptions>(fileName).Write(options);
    }
}

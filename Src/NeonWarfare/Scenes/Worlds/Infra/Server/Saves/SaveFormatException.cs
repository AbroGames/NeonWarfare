using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// A save cannot be read because it is truncated or broken.
/// A save of another version throws the subclass <see cref="SaveVersionMismatchException"/>.
/// </summary>
public class SaveFormatException : Exception
{
    public SaveFormatException(string message) : base(message) { }

    public SaveFormatException(string message, Exception innerException) : base(message, innerException) { }
}

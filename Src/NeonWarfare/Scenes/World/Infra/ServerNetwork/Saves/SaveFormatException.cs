using System;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;

/// <summary>
/// A save cannot be read: it is truncated, broken, or of another version (<see cref="SaveVersionMismatchException"/>).
/// </summary>
public class SaveFormatException : Exception
{
    public SaveFormatException(string message) : base(message) { }

    public SaveFormatException(string message, Exception innerException) : base(message, innerException) { }
}

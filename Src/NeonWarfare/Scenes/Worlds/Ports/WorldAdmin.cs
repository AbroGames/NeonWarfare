
namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// Who becomes admin on joining a server World: the host itself, or <c>--admin</c> of a dedicated server.
/// </summary>
/// <param name="Uid"><c>null</c> — the World has no admin until another admin grants the rights.</param>
public record WorldAdmin(string Uid);

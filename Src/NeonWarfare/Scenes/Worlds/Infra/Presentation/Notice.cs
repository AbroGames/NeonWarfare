
namespace NeonWarfare.Scenes.Worlds.Infra.Presentation;

/// <summary>
/// The base of a HUD notice, not an <c>Event</c>: a notice never leaves the process and lives for one frame, see
/// <see cref="HudMailbox"/>.
/// </summary>
public abstract record Notice;

using NeonWarfare.Scenes.Worlds.Infra.Presentation;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

/// <summary>
/// A chat entry has been added. The notice carries no entry, because the HUD re-reads the whole history from
/// <see cref="ChatPresentation"/>.
/// </summary>
public record ChatEntryAddedNotice : Notice;

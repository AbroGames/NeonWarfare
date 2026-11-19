using NeonWarfare.Scenes.Worlds.Infra.Presentation;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

// No entry inside: the HUD reads the history from ChatPresentation, so the notice cannot go stale
public record ChatEntryAddedNotice : Notice;

using NeonWarfare.Scenes.World.Infra.Hud;

namespace NeonWarfare.Scenes.World.Features.Chat;

// No entry inside: the HUD reads the history from ChatPresentation, so the notice cannot go stale
public record ChatEntryAddedNotice : Notice;

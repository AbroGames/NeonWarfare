namespace NeonWarfare.Scenes.World.Notices;

// No entry inside: the HUD reads the history from ChatPresentation, so the notice cannot go stale
public record ChatEntryAddedNotice : Notice;

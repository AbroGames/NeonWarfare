namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// The base of every event, for typing only. The events read from the network are limited by the set of mapped event
/// types.
/// </summary>
public abstract record Event;

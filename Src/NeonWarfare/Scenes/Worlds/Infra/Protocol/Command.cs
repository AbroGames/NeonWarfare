namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// The base of every command, for typing only. The commands accepted from the network are limited by the whitelist of
/// the found handlers.
/// </summary>
public abstract record Command;

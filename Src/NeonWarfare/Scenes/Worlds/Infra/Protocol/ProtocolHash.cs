namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// The <see cref="ProtocolHasher"/> hash of this build. A peer or a save with another hash is refused.
/// </summary>
public record ProtocolHash(ulong Value);

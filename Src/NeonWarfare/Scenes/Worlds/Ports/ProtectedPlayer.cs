namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The player whose process owns the server: the host itself, or the client that started a child server.
/// A join with this uid while it is online is rejected instead of displacing it, see Docs/Networking.md#join-and-leave.
/// </summary>
/// <param name="Uid">When it is <c>null</c>, every player can be displaced.</param>
public record ProtectedPlayer(PlayerUid? Uid);

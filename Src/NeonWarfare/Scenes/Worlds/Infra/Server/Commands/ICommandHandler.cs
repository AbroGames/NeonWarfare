namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

/// <summary>
/// The base of every command handler interface.
/// The handlers of different commands have no common generic type, so a consumer takes them all as
/// <c>IEnumerable&lt;ICommandHandler&gt;</c>.
/// </summary>
public interface ICommandHandler;

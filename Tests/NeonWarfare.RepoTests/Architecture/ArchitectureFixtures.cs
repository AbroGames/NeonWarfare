namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// Stubs of the game types the architecture rules look for, under the game's full names, for the sources of
/// <see cref="Infrastructure.GameAssembly.Compile"/>. Only the names matter: a rule reads metadata, never runs it.
/// </summary>
public static class ArchitectureFixtures
{
    public const string LayerAttributes = """
        namespace NeonWarfare.Scenes.Worlds.Infra.Composition
        {
            public abstract class WorldServiceAttribute : System.Attribute;
            public class SimulationAttribute : WorldServiceAttribute;
            public class SimulationFacadeAttribute : WorldServiceAttribute;
            public class CommandHandlerAttribute : WorldServiceAttribute;
            public class ServerAttribute : WorldServiceAttribute;
            public class QueryAttribute : WorldServiceAttribute;
            public class ClientAttribute : WorldServiceAttribute;
            public class PresentationAttribute : WorldServiceAttribute;
            public class ClientReplicationAttribute : WorldServiceAttribute;
        }

        namespace NeonWarfare.Scenes.Worlds.Infra.Client.Events
        {
            public class EventHandlerAttribute : System.Attribute;
            public interface IEventHandlerOwner;
        }

        namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands
        {
            public interface ICommandHandler;
            public interface IPlayerCommandHandler<TCommand> : ICommandHandler;
        }

        """;
}

using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Composition;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class WorldServiceAttribute(WorldLayer layer) : Attribute
{
    public WorldLayer Layer => layer;
}

public class SimulationAttribute() : WorldServiceAttribute(WorldLayer.Simulation);

public class SimulationFacadeAttribute() : WorldServiceAttribute(WorldLayer.SimulationFacade);

public class CommandHandlerAttribute() : WorldServiceAttribute(WorldLayer.CommandHandler);

public class ServerAttribute() : WorldServiceAttribute(WorldLayer.Server);

public class QueryAttribute() : WorldServiceAttribute(WorldLayer.Query);

public class ClientAttribute() : WorldServiceAttribute(WorldLayer.Client);

public class PresentationAttribute() : WorldServiceAttribute(WorldLayer.Presentation);

public class ClientReplicationAttribute() : WorldServiceAttribute(WorldLayer.ClientReplication);

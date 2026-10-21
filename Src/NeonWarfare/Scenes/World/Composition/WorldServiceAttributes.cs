using System;

namespace NeonWarfare.Scenes.World.Composition;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class WorldServiceAttribute(WorldLayer layer) : Attribute
{
    public WorldLayer Layer => layer;
}

public class SimulationAttribute() : WorldServiceAttribute(WorldLayer.Simulation);

public class SimulationFacadeAttribute() : WorldServiceAttribute(WorldLayer.SimulationFacade);

public class CommandHandlerAttribute() : WorldServiceAttribute(WorldLayer.CommandHandler);

public class ServerNetworkAttribute() : WorldServiceAttribute(WorldLayer.ServerNetwork);

public class QueryAttribute() : WorldServiceAttribute(WorldLayer.Query);

public class ClientNetworkAttribute() : WorldServiceAttribute(WorldLayer.ClientNetwork);

public class PresentationAttribute() : WorldServiceAttribute(WorldLayer.Presentation);

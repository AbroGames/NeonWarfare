using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Composition;

[Flags]
public enum WorldLayer
{
    None = 0,

    Simulation = 1 << 0,
    SimulationFacade = 1 << 1,
    CommandHandler = 1 << 2,
    ServerNetwork = 1 << 3,

    Query = 1 << 4,

    ClientNetwork = 1 << 5,
    Presentation = 1 << 6,
    ClientReplication = 1 << 7,

    Dedicated = Simulation | SimulationFacade | CommandHandler | ServerNetwork | Query,
    Client = Query | ClientNetwork | Presentation | ClientReplication,
    // Host = Dedicated + Client - ClientReplication
    // No ClientReplication: the host's Simulation already wrote the state, which is replicated to remote clients
    Host = (Dedicated | Client) & ~ClientReplication
}

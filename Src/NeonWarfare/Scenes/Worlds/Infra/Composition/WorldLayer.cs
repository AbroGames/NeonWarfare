using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Composition;

[Flags]
public enum WorldLayer
{
    None = 0,

    Simulation = 1 << 0,
    SimulationFacade = 1 << 1,
    CommandHandler = 1 << 2,
    Server = 1 << 3,

    Query = 1 << 4,

    Client = 1 << 5,
    Presentation = 1 << 6,
    ClientReplication = 1 << 7
}

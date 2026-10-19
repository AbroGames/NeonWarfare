using System;

namespace NeonWarfare.Scenes.World.Composition;

// The starter lists every layer it needs: Simulation does not pull in its SimulationFacade, CommandHandler,
// ServerNetwork and Query by itself. A Presentation the ServerHud needs is a layer of its own, so the full
// presentation set is Presentation | ServerHudPresentation. DedicatedWindow is passed only by a dedicated server
// with ServerHud: the host also takes ServerHudPresentation, so the window cannot be inferred from the other layers.
// ClientNetwork is passed by every configuration with a Presentation: it delivers the received events to it
[Flags]
public enum WorldLayer
{
    None = 0,

    Simulation = 1 << 0,
    SimulationFacade = 1 << 1,
    CommandHandler = 1 << 2,
    ServerNetwork = 1 << 3,
    DedicatedWindow = 1 << 4,

    Query = 1 << 5,

    ClientNetwork = 1 << 6,
    Presentation = 1 << 7,
    ServerHudPresentation = 1 << 8
}

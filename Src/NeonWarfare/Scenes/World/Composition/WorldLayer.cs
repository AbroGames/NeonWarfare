using System;

namespace NeonWarfare.Scenes.World.Composition;

// The starter lists every layer it needs: Simulation does not pull in its CommandHandler, ServerNetwork
// and Query by itself. A Presentation the ServerHud needs is a layer of its own, so the full
// presentation set is Presentation | ServerHudPresentation. Console is passed only by a dedicated server with
// ServerHud: the host also takes ServerHudPresentation, so the console cannot be inferred from the other layers
[Flags]
public enum WorldLayer
{
    None = 0,
    Simulation = 1 << 0,
    CommandHandler = 1 << 1,
    ServerNetwork = 1 << 2,
    Query = 1 << 3,
    Presentation = 1 << 4,
    ServerHudPresentation = 1 << 5,
    Console = 1 << 6
}

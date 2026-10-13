using System;

namespace NeonWarfare.Scenes.World.Composition;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class WorldServiceAttribute : Attribute
{
    public abstract WorldLayer Layer { get; }
}

public class SimulationAttribute : WorldServiceAttribute
{
    public bool Facade { get; init; }

    public override WorldLayer Layer => WorldLayer.Simulation;
}

public class CommandHandlerAttribute : WorldServiceAttribute
{
    public override WorldLayer Layer => WorldLayer.CommandHandler;
}

public class ServerNetworkAttribute : WorldServiceAttribute
{
    public override WorldLayer Layer => WorldLayer.ServerNetwork;
}

public class QueryAttribute : WorldServiceAttribute
{
    public override WorldLayer Layer => WorldLayer.Query;
}

public class PresentationAttribute : WorldServiceAttribute
{
    public bool RequiredByServerHud { get; init; }

    public override WorldLayer Layer =>
        RequiredByServerHud ? WorldLayer.ServerHudPresentation : WorldLayer.Presentation;
}

public class ConsoleAttribute : WorldServiceAttribute
{
    public override WorldLayer Layer => WorldLayer.Console;
}

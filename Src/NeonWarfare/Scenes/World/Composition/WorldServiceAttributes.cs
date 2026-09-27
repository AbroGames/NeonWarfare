using System;

namespace NeonWarfare.Scenes.World.Composition;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class WorldServiceAttribute : Attribute
{
    public abstract bool AnnotatedClassBelongsTo(WorldServiceGroups groups);
}

public class SimulationAttribute : WorldServiceAttribute
{
    public bool Facade { get; init; }

    public override bool AnnotatedClassBelongsTo(WorldServiceGroups groups) => groups.Simulation;
}

public class CommandHandlerAttribute : WorldServiceAttribute
{
    public override bool AnnotatedClassBelongsTo(WorldServiceGroups groups) => groups.Simulation;
}

public class PresentationAttribute : WorldServiceAttribute
{
    public bool RequiredByServerHud { get; init; }

    public override bool AnnotatedClassBelongsTo(WorldServiceGroups groups)
    {
        if (groups.Presentation == PresentationScope.Full)
        {
            return true;
        }
        if (groups.Presentation == PresentationScope.RequiredByServerHud)
        {
            return RequiredByServerHud;
        }
        return false;
    }
}

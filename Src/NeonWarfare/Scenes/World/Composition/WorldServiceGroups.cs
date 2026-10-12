namespace NeonWarfare.Scenes.World.Composition;

// Named by consumer, not by launch mode: client (false, Full), host (true, Full),
// dedicated server with ServerHud (true, RequiredByServerHud), headless dedicated server (true, None)
public record WorldServiceGroups(bool Simulation, PresentationScope Presentation)
{
    public bool HasServerHud => Simulation && Presentation == PresentationScope.RequiredByServerHud;
}

public enum PresentationScope
{
    None,
    RequiredByServerHud,
    Full
}

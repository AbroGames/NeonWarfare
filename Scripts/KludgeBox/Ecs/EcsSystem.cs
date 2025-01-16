using NeonWarfare.Scripts.KludgeBox.Ecs.Systems.Interfaces;

namespace NeonWarfare.Scripts.KludgeBox.Ecs;

public abstract class EcsSystem : ISystem
{
    public EcsWorld World { get; set; }
    public bool Enabled { get; set; } = true;
}

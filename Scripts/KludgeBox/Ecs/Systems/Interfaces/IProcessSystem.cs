namespace NeonWarfare.Scripts.KludgeBox.Ecs.Systems.Interfaces;

public interface IProcessSystem : ISystem
{
    void Process(double delta);
}

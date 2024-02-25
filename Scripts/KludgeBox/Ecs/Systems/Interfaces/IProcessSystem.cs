namespace KludgeBox.Ecs;

public interface IProcessSystem : ISystem
{
    void Process(double delta);
}
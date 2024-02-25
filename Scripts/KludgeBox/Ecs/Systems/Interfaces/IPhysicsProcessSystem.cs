namespace KludgeBox.Ecs;

public interface IPhysicsProcessSystem : ISystem
{
    void PhysicsProcess(double delta);
}
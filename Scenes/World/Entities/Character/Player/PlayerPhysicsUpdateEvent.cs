public class PlayerPhysicsUpdateEvent(Player player, double delta)
{
    public Player Player { get; private set; } = player;
    public double Delta { get; private set; } = delta;
}
public readonly struct PlayerPhysicsProcessEvent(Player player, double delta)
{
    public Player Player { get; } = player;
    public double Delta { get; } = delta;
}
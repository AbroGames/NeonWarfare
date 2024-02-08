//TODO генератор конструктора или генератор билдера, как в lombok? 
public readonly struct PlayerProcessEvent(Player player, double delta)
{
    public Player Player { get; } = player;
    public double Delta { get; } = delta;
}
//TODO генератор конструктора или генератор билдера, как в lombok? 
public class PlayerUpdateEvent(Player player, double delta)
{
    public Player Player { get; private set; } = player;
    public double Delta { get; private set; } = delta;
}
public class PlayerGainXpEvent(Player player, int xp)
{
    public Player Player { get; private set; } = player;
    public int Xp { get; private set; } = xp;
}
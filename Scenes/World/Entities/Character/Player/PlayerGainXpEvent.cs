public readonly struct PlayerGainXpEvent(Player player, int xp)
{
    public Player Player { get; } = player;
    public int Xp { get; } = xp;
}
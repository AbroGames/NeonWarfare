public readonly struct PlayerReadyEvent(Player player)
{
    public Player Player { get; } = player;
}
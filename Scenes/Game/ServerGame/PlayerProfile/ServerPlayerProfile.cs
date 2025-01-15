using NeonWarfare;

public class ServerPlayerProfile
{
    public long PeerId { get; private set; }
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public ServerPlayer Player => ServerRoot.Instance.Game.World.PlayerByPeerId[PeerId];

    public ServerPlayerProfile(long peerId)
    {
        PeerId = peerId;
    }
}
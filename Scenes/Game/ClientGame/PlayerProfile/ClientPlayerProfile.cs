using NeonWarfare;

public class ClientPlayerProfile
{
    public long Id { get; private set; }
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public Player Player => ClientRoot.Instance.Game.World.Player;

    public ClientPlayerProfile(long id)
    {
        Id = id;
    }
}
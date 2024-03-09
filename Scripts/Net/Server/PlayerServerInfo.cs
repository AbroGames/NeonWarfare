namespace NeoVector;

public class PlayerServerInfo
{
    public long Id { get; private set; }
    public string Nickname { get; private set; } = "";
    public bool IsAdmin { get; set; } = false;

    public Player Player;

    public PlayerServerInfo(long id)
    {
        Id = id;
    }
}
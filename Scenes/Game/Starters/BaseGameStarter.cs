namespace NeonWarfare.Scenes.Game.Starters;

public abstract class BaseGameStarter
{
    protected const string Localhost = "127.0.0.1";
    protected const string DefaultHost = Localhost;
    protected const int DefaultPort = 25566;

    public virtual void Init(Game game)
    {
        
    }
}

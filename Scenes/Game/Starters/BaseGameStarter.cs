using System;
using NeonWarfare.Scripts.Service;

namespace NeonWarfare.Scenes.Game.Starters;

public abstract class BaseGameStarter
{
    protected const string Localhost = "127.0.0.1";
    protected const string DefaultHost = Localhost;
    protected const int DefaultPort = 25566;

    public virtual void Init(Game game) { }

    protected void ServerStartWorld(World.World world, string saveFileName, string adminNickname)
    {
        if (saveFileName == null)
        {
            world.ServerStartStopService.StartNewGame(adminNickname);
        }
        else
        {
            try
            {
                world.ServerStartStopService.LoadGame(saveFileName, adminNickname);
            }
            catch (SaveLoadService.LoadException loadException)
            {
                Net.DoClient(() => GoToMenuAndShowError(loadException.Message));
            }
        }
    }

    protected void ClientStartWorld(World.World world)
    {
        world.ClientStartStopService.StartSyncWithServer(GoToMenuAndShowError);
    }

    /// <summary>
    /// This method calls only on client.<br/>
    /// Log error message to logger must be early, because this method calls only on client,
    /// but we want log error message on client and server. So, we can't log error message here.
    /// </summary>
    protected void GoToMenuAndShowError(string message)
    {
        if (!Net.IsClient()) throw new InvalidOperationException("Can only be executed on the client");
        
        Services.MainScene.StartMainMenu(message);
        Services.LoadingScreen.Clear();
    }
}

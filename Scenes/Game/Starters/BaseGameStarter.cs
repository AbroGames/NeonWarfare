using System;
using NeonWarfare.Scripts.Service;

namespace NeonWarfare.Scenes.Game.Starters;

public abstract class BaseGameStarter
{
    protected const string DefaultHost = Consts.DefaultHost;
    protected const int DefaultPort = Consts.DefaultPort;

    public abstract void Init(Game game);

    protected void SetLastGame(ResumableGame lastGame)
    {
        Services.LastGame.SetLastGame(lastGame);
    }

    protected void AddLastGameUpdaterToSaveEvent(World.World world, ResumableGame lastGame)
    {
        world.DataSaveLoadService.SaveSuccessServerEvent += saveName =>
        {
            Services.LastGame.SetLastGame(lastGame with { SaveName = saveName });
        };
    }
    
    protected void ServerStartWorld(World.World world, string saveFileName, string adminUid)
    {
        saveFileName ??= Services.SaveLoad.GenNewSaveFileName();

        if (!Services.SaveLoad.CheckFileExists(saveFileName))
        {
            world.ServerStartStopService.StartNewGame(saveFileName, adminUid);
        }
        else
        {
            try
            {
                world.ServerStartStopService.LoadGame(saveFileName, adminUid);
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

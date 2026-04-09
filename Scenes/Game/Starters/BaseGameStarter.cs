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
        Services.GameSettings.SetSettings(Services.GameSettings.GetSettings() with { LastGame = lastGame });
    }

    protected void AddLastGameUpdaterToSaveEvent(World.World world, ResumableGame lastGame)
    {
        world.DataSaveLoadService.SaveSuccessServerEvent += saveName =>
        {
            Services.GameSettings.SetSettings(Services.GameSettings.GetSettings() 
                with { LastGame = lastGame with { SaveName = saveName } });
        };
        //TODO надо проверить утечки памяти потом! И/или сделать авто-отвязку.
        //TODO Сразу реализовать обязательный выбор имени сохранения для игры при старте? Дефолтный забитый вариант: дата и время (без секунд)
        //TODO В HostMultiplayerGameStarter четкие флаги: ServerHud (его же в параметры дедика как no-hud), SetLastGame
        //TODO Убрать все ? = null из конструкторов стартеров, в MainScene постараться тоже убрать
        //TODO Сделать кнопку
        //TODO Скопировать ResumableGame (отдельный файл) из неонки, скопировать логику из MainSceneService 
        //TODO Коммент <param name="worldRender">Show not the GUI, but the game scene</param>
    }
    
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

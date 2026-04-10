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
        //TODO Коммент <param name="worldRender">Show not the GUI, but the game scene</param>  !!!!! and serverHudRender!
        //TODO Передавать на фронт в списке сейвов дату последнего изменения файла
        //TODO Перенести в неонку все изменения с 9 апреля
        //TODO Поменять во всех скриптах запуска флаги на world-render, uid
        
        //TODO Делать только в неонке? Но потом uid можно заменить на стим будет, так что выглядит полезно
        //TODO uid UUIDv4? Без timestamp 
        //TODO uid вместо ника игрока на сервере как ключ мапы, команда для передачи PlayerData к новому игроку
        //TODO uid в настройки, флаг cmd для смены uid, комманда сервера для перепривязки игрока к новому uid
        //TODO логирование при синке и т.п. вместе с ником в скобках uid
        //TODO Убрать валидацию на дубликат ников
    }
    
    protected void ServerStartWorld(World.World world, string saveFileName, string adminNickname)
    {
        saveFileName ??= Services.SaveLoad.GenNewSaveFileName();

        if (!Services.SaveLoad.CheckFileExists(saveFileName))
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

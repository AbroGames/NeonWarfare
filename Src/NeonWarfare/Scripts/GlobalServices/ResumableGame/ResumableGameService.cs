using KludgeBox.Persistence;

namespace NeonWarfare.Scripts.GlobalServices.ResumableGame;

public class ResumableGameService
{
    private const string DefaultFileName = "resume-game.json";

    private readonly JsonFile<ResumableGame> _file;

    public ResumableGameService(string fileName = DefaultFileName)
    {
        _file = new JsonFile<ResumableGame>(fileName);
    }

    /// <summary>
    /// Reads the file on every call, because a child dedicated server rewrites it after a "save as".
    /// A missing or bad file is <see cref="ResumableGame.ResumableType.None"/> and is never rewritten by a read.
    /// </summary>
    public ResumableGame GetLastGame()
    {
        JsonFileRead<ResumableGame> read = _file.Read(game => game.IsValid());
        return read.Outcome == JsonFileOutcome.Loaded ? read.Value : ResumableGame.GetNone();
    }

    /// <summary>
    /// A failed write is logged and not thrown.
    /// </summary>
    public void SetLastGame(ResumableGame lastGame)
    {
        _file.Write(lastGame);
    }

    public void StartLastGame()
    {
        ResumableGame game = GetLastGame();
        switch (game.Type)
        {
            case ResumableGame.ResumableType.RunSingleplayer:
                Services.MainScene.StartSingleplayerGame(new SaveFileName(game.SaveName));
                break;
            case ResumableGame.ResumableType.ConnectToServer:
                Services.MainScene.ConnectToMultiplayerGame(game.Host, game.Port);
                break;
            case ResumableGame.ResumableType.CreateServer:
                Services.MainScene.HostMultiplayerGameAsClient(
                    new SaveFileName(game.SaveName), game.Port, game.IsDedicated!.Value, showWindow: true);
                break;
        }
    }
}

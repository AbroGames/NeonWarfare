namespace NeonWarfare.Scenes.OldWorld.WorldServices.Command;

public interface ICommandProcessor
{

    string GetCommand();
    bool IsRequiringAdmin();
    string GetDescription();
    void ProcessCommand(int senderId, string command, World world);
}
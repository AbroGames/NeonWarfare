using Godot;
using KludgeBox;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ClientRoot
{
	
	//TODO можно создать сервер несколько раз за игру. Лучше привязать ServerShutdowner куда-то в Network и при каждом запуске сервра или подключение по сети пересоздавать объект Network
	//TODO таким образом можно по дефолту вообще Network не инициализировать, а делать это только при подключение
	//TODO в идеале всю эту логику поместить в финалайзер (метод Free) ноды Game или подобной, которая общая для BattleWorld и SafeWorld, но не используется в MainMenu
	//TODO и переместить в другое место сам файл ServerSutdowner
    public ServerShutdowner ServerShutdowner { get; private set; }

	public void AddServerShutdowner(int serverPid)
	{
		ServerShutdowner?.QueueFree();
		ServerShutdowner = new ServerShutdowner();
		ServerShutdowner.ServerPid = serverPid;
		AddChild(ServerShutdowner);
	}
}
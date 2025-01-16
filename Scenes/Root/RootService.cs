using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Events.Global;
using NeonWarfare.Scripts.Utils;
using NeonWarfare.Scripts.Utils.CmdArgs;

namespace NeonWarfare.Scenes.Root;

public static class RootService
{
	public static void CommonInit(ListenerSide listenerSide)
	{
		ExceptionHandlerService.AddExceptionHandlerForUnhandledException();
		CmdArgsService.LogCmdArgs();
		EventBus.Init(listenerSide);
	}
}

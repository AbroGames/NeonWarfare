using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;

namespace NeonWarfare.Scenes.World.ServerNetwork;

/// <summary>
/// Sends the commands of the <c>ServerHud</c> window: it exists only on a dedicated server with not --headless mode.
/// </summary>
[DedicatedWindow]
public class DedicatedWindowCommandSender(CommandInbox inbox)
{
    // Generic only so that an architecture test sees the command type at every call site and can require a
    // dedicated-window handler for it
    public void Send<TCommand>(TCommand command) where TCommand : Command
    {
        inbox.EnqueueFromDedicatedWindow(command);
    }
}

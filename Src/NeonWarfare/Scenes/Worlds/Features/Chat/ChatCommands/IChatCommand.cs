namespace NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;

/// <summary>
/// A chat command, see Docs/Chat-and-commands.md#chat-commands.
/// Only <c>/help</c> implements it directly, every other command implements <see cref="IListedChatCommand"/>.
/// </summary>
public interface IChatCommand
{
    /// <summary>What follows the <c>/</c>: lower case, with no whitespace.</summary>
    string Name { get; }
    string Description { get; }
    bool RequiresAdmin { get; }

    void Execute(PlayerUid senderUid, string arguments);
}

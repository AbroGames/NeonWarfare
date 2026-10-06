using MessagePack;

namespace NeonWarfare.Scenes.World.Commands;

[MessagePackObject]
public record SendChatMessageCommand(
    [property: Key(0)] string Text) : Command;

using MessagePack;
using NeonWarfare.Scenes.World.Infra.Protocol;

namespace NeonWarfare.Scenes.World.Features.Chat;

[MessagePackObject]
public record SendChatMessageCommand(
    [property: Key(0)] string Text) : Command;

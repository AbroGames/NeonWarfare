using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[MessagePackObject]
public record SendChatMessageCommand(
    [property: Key(0)] string Text) : Command;

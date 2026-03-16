using MessagePack;

namespace NeonWarfare.Scenes.World.Service.Chat;

[MessagePackObject]
public record ChatMessage
{
    [Key(0)] public int SenderId;
    [Key(1)] public string Nick;
    [Key(2)] public string Text;
}
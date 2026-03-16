namespace NeonWarfare.Scenes.World.Service.Chat;

public interface IChatMessageInterceptor
{

    public bool IsPass(int senderId, string text);
}
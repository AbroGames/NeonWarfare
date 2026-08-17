namespace NeonWarfare.Scenes.World.WorldServices.Chat;

public interface IChatMessageInterceptor
{

    public bool IsPass(int senderId, string text);
}
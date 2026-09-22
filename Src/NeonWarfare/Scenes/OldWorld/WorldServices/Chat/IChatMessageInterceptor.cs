namespace NeonWarfare.Scenes.OldWorld.WorldServices.Chat;

public interface IChatMessageInterceptor
{

    public bool IsPass(int senderId, string text);
}
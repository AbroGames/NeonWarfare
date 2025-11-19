using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Game.ServerGame.ServerCommandsService;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.Screen;

public static class ChatNetworking
{
    [GamePacket]
    public class CS_SendMessagePacket(string text) : BinaryPacket
    {
        public override int PreferredChannel => 1;

        public string MessageText= text;
    }
    
    [GamePacket]
    public class SC_SendMessagePacket(string text, long authorId) : BinaryPacket
    {
        public override int PreferredChannel => 1;
        
        public string MessageText = text;
        public long AuthorId = authorId;
    }
    
    
    
    [EventListener(ListenerSide.Client)]
    public static void OnMessageReceivedFromServer(SC_SendMessagePacket packet)
    {
        if (ClientRoot.Instance.Game.Hud != null)
        {
            ClientRoot.Instance.Game.Hud.ChatContainer.ReceiveMessage(ChatMessage.FromPacket(packet));
        }
    }

    
    [EventListener(ListenerSide.Server)]
    public static void OnMessageReceivedFromClient(CS_SendMessagePacket packet)
    {
        if (packet.MessageText.StartsWith("/class "))
        {
            bool res = ServerRoot.Instance.Game.PlayerProfilesByPeerId[packet.SenderId].ChangeClass(packet.MessageText.Substring("/class ".Length));
            if (res)
            {
                Network.SendToAll(new SC_SendMessagePacket($"Класс успешно изменен на {packet.MessageText.Substring("/class ".Length)}", packet.SenderId));
            }
            else
            {
                Network.SendToAll(new SC_SendMessagePacket($"Некорректное название класса {packet.MessageText.Substring("/class ".Length)}", packet.SenderId));
            }
            return;
        }
        
        if (packet.MessageText.StartsWith("/wave "))
        {
            bool res = ServerRoot.Instance.Game.GameSettings.SetWaveType(packet.MessageText.Substring("/wave ".Length));
            if (res)
            {
                Network.SendToAll(new SC_SendMessagePacket($"Тип волны успешно изменен на {packet.MessageText.Substring("/wave ".Length)}", packet.SenderId));
            }
            else
            {
                Network.SendToAll(new SC_SendMessagePacket($"Некорректный тип волны {packet.MessageText.Substring("/wave ".Length)}", packet.SenderId));
            }
            return;
        }
        
        if (packet.MessageText.StartsWith("/inc "))
        {
            bool res = ServerRoot.Instance.Game.GameSettings.SetWaveInc(packet.MessageText.Substring("/inc ".Length));
            if (res)
            {
                Network.SendToAll(new SC_SendMessagePacket($"Рост волн успешно изменен на {packet.MessageText.Substring("/inc ".Length)}", packet.SenderId));
            }
            else
            {
                Network.SendToAll(new SC_SendMessagePacket($"Некорректный тип значения роста волн {packet.MessageText.Substring("/inc ".Length)}", packet.SenderId));
            }
            return;
        }
        
        Network.SendToAll(new SC_SendMessagePacket(packet.MessageText, packet.SenderId));
    }
}

public record ChatMessage(string MessageText, SenderInfo SenderInfo)
{
    public static ChatMessage FromPacket(ChatNetworking.SC_SendMessagePacket packet)
    {
        var senderProfile = packet.AuthorId == -1 ? null : ClientRoot.Instance.Game.AllyProfilesByPeerId[packet.AuthorId];
        var message = new ChatMessage(
            MessageText: packet.MessageText,
            SenderInfo: packet.AuthorId == -1 
                ? SenderInfo.System
                : new SenderInfo(packet.AuthorId, senderProfile.Name, senderProfile.Color, senderProfile.IsAdmin)
            );
        
        return message;
    }

    public static ChatMessage FromPacket(ChatNetworking.CS_SendMessagePacket packet)
    {
        var senderProfile = ServerRoot.Instance.Game.PlayerProfilesByPeerId[packet.SenderId];
        var message = new ChatMessage(
            MessageText: packet.MessageText,
            SenderInfo: packet.SenderId == -1 
                ? SenderInfo.System
                : new SenderInfo(packet.SenderId, senderProfile.Name, senderProfile.Color, senderProfile.IsAdmin)
        );
        
        return message;
    }
}

public record SenderInfo(long AuthorId, string SenderName, Color SenderColor, bool IsAdmin)
{
    public static SenderInfo System { get; } = new (-1, "SERVER", Colors.White, true);
    
    public bool IsSystem => AuthorId == -1;
}
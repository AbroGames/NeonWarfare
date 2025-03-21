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
        public string MessageText= text;
    }
    
    [GamePacket]
    public class SC_SendMessagePacket(string text, long authorId) : BinaryPacket
    {
        public string MessageText = text;
        public long AuthorId = authorId;
    }
    
    
    
    [EventListener(ListenerSide.Client)]
    public static void OnMessageReceivedFromServer(SC_SendMessagePacket packet)
    {
        ClientRoot.Instance.Game.Hud.ChatContainer.ReceiveMessage(ChatMessage.FromPacket(packet));
    }

    
    [EventListener(ListenerSide.Server)]
    public static void OnMessageReceivedFromClient(CS_SendMessagePacket packet)
    {
        if (packet.MessageText.StartsWith('/'))
        {
            ServerRoot.Instance.Game.CommandsService.TryExecuteCommand(ChatMessage.FromPacket(packet));
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
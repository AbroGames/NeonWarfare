using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Service.Chat;

public partial class WorldChatService : Node
{
    private const int MaxNumberOfMessages = 100;
    private const string ServerNick = "SERVER";
    
    public event Action<ChatMessage> SentNewMessageEvent;
    public IReadOnlyCollection<ChatMessage> Messages => _messages;
    
    private readonly Queue<ChatMessage> _messages = new(MaxNumberOfMessages);
    private readonly List<IChatMessageInterceptor> _interceptors = new();
    
    [SceneService] private WorldFacadeService _facadeService;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void TrySendNewMessage(string text, int receiverId = BroadcastId) => RpcId(ServerId, MethodName.TrySendNewMessageRpc, text, receiverId);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferChannel = (int) Consts.TransferChannel.Chat)]
    private void TrySendNewMessageRpc(string text, int receiverId)
    {
        int senderId = GetMultiplayer().GetRemoteSenderId();
        
        foreach (var interceptor in _interceptors)
        {
            if (!interceptor.IsPass(senderId, text)) return;
        }
        
        string senderNick = senderId == 1 ? ServerNick : _facadeService.GetPlayerData(senderId).Nick;
        ChatMessage chatMessage = new ChatMessage
        {
            SenderId = senderId,
            Nick = senderNick,
            Text = text
        };
        NotifyAboutNewMessage(chatMessage, receiverId);
    }
    
    private void NotifyAboutNewMessage(ChatMessage chatMessage, int receiverId) => RpcId(receiverId, MethodName.NotifyAboutNewMessageRpc, Serialize(chatMessage));
    [Rpc(CallLocal = true, TransferChannel = (int) Consts.TransferChannel.Chat)]
    private void NotifyAboutNewMessageRpc(byte[] chatMessageBytes)
    {
        ChatMessage chatMessage = Deserialize<ChatMessage>(chatMessageBytes);

        if (_messages.Count >= MaxNumberOfMessages) _messages.Dequeue();
        _messages.Enqueue(chatMessage);
        SentNewMessageEvent?.Invoke(chatMessage);
    }

    public void AddInterceptor(IChatMessageInterceptor interceptor) => _interceptors.Add(interceptor);
    public void RemoveInterceptor(IChatMessageInterceptor interceptor) => _interceptors.Remove(interceptor);
    public bool HasInterceptor(IChatMessageInterceptor interceptor) => _interceptors.Contains(interceptor);
}
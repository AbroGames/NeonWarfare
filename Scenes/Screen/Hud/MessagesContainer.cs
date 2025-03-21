using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class MessagesContainer : VBoxContainer
{
    public int MaxMessagesHistory { get; set; } = 100;
    
    [Export] [NotNull] private PackedScene MessageScene { get; set; }
    
    private Queue<Message> _messageQueue = new Queue<Message>();
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        _messageQueue.EnsureCapacity(MaxMessagesHistory);
    }

    public float GetDesiredHeight()
    {
        var sum = _messageQueue.Where(msg => !msg.IsExpired).Sum(msg => msg.Size.Y);
        return sum;
    }

    public void AppendMessage(ChatMessage chatMessage)
    {
        var messageNode = MessageScene.Instantiate<Message>();

        if (chatMessage.SenderInfo.IsSystem)
        {
            messageNode.InitMessageRaw(chatMessage.MessageText);
        }
        else
        {
            messageNode.InitMessage(chatMessage);
        }
        
        AddChild(messageNode);
        _messageQueue.Enqueue(messageNode);
        if (_messageQueue.Count > MaxMessagesHistory)
        {
            var messageToRemove = _messageQueue.Dequeue();
            messageToRemove.QueueFree();
        }

        foreach (var message in _messageQueue.SkipLast(5))
        {
            message.ForceExpiration();
        }
    }

    public void SetForcedVisibility(bool visible)
    {
        foreach (var message in _messageQueue)
        {
            message.SetForcedVisibility(visible);
        }
    }
}

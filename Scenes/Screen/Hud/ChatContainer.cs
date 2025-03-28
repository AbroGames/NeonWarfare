using Godot;
using System;
using System.Collections.Generic;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;
using NeonWarfare.Scripts.KludgeBox.Networking;

public partial class ChatContainer : MarginContainer
{
    [Export] [NotNull] private MessagesContainer MessagesContainer { get; set; }
    [Export] [NotNull] private ScrollContainer ScrollContainer { get; set; }
    [Export] [NotNull] private LineEdit MessageInputBox { get; set; }
    [Export] [NotNull] private Control InputSpacePlaceholder { get; set; }
    
    public bool IsOpened { get; private set; } = false;

    private List<string> _history = new();
    private string _lastMessage = "";
    private int _historyIndex = -1;
    private const float MaxChatHeight = 350;
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed(Keys.Chat) && !IsOpened)
        {
            OpenChat();
            return;
        }

        if (@event.IsActionPressed(Keys.Cancel) && IsOpened)
        {
            CloseChat();
            return;
        }
        
        if (@event.IsActionPressed(Keys.Enter) && IsOpened)
        {
            TrySendMessage();
            return;
        }

        if (@event.IsActionPressed(Keys.UiUp) && IsOpened)
        {
            TryGetPreviousMessage();
        }

        if (@event.IsActionPressed(Keys.UiDown) && IsOpened)
        {
            TryGetNextMessage();
        }
    }

    public override void _Process(double delta)
    {
        var desiredHeight = MessagesContainer.GetDesiredHeight();
        desiredHeight = Mathf.Min(desiredHeight, MaxChatHeight);
        
        var height = IsOpened ? MaxChatHeight : desiredHeight;
        ScrollContainer.CustomMinimumSize = new Vector2(0, height);
    }

    private void TryGetPreviousMessage()
    {
        if (_historyIndex == -1)
        {
            _lastMessage = MessageInputBox.Text; // Сохраняем текущий ввод перед просмотром истории
            _historyIndex = _history.Count; // Начинаем с последнего сообщения
        }

        if (_historyIndex > 0)
        {
            _historyIndex--; 
            MessageInputBox.Text = _history[_historyIndex];
        }
    }

    private void TryGetNextMessage()
    {
        if (_historyIndex == -1)
            return;

        _historyIndex++;

        if (_historyIndex < _history.Count)
        {
            MessageInputBox.Text = _history[_historyIndex];
        }
        else
        {
            _historyIndex = -1;
            MessageInputBox.Text = _lastMessage; // Вернуть текст, который был перед тем, как начал листать
        }
    }

    public void OpenChat()
    {
        ClientPlayer.IsInputBlocked = true;
        MessageInputBox.Text = "";
        MessageInputBox.Visible = true;
        InputSpacePlaceholder.Visible = false;
        MessageInputBox.GrabFocus();
        IsOpened = true;
        MessagesContainer.SetForcedVisibility(true);
        ScrollContainer.ScrollVertical = Int32.MaxValue;
        _historyIndex = -1;
        _lastMessage = "";
    }

    public void CloseChat()
    {
        ClientPlayer.IsInputBlocked = false;
        MessageInputBox.Text = "";
        MessageInputBox.Visible = false;
        InputSpacePlaceholder.Visible = true;
        IsOpened = false;
        MessagesContainer.SetForcedVisibility(false);
        ScrollContainer.ScrollVertical = Int32.MaxValue;
    }

    public void TrySendMessage()
    {
        if (String.IsNullOrWhiteSpace(MessageInputBox.Text))
        {
            CloseChat();
            return;
        }
        
        Network.SendToServer(new ChatNetworking.CS_SendMessagePacket(MessageInputBox.Text));
        _history.Add(MessageInputBox.Text);
        MessageInputBox.Text = "";
        CloseChat();
    }

    public void ReceiveMessage(ChatMessage message)
    {
        Audio2D.PlayUiSound(Sfx.UiSelect);
        MessagesContainer.AppendMessage(message);
    }
}

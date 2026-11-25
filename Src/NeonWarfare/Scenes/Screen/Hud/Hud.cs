using System;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Features.Saves;
using NeonWarfare.Scenes.World.Infra.Hud;
using Serilog;

namespace NeonWarfare.Scenes.Screen.Hud;

public partial class Hud : Control
{
    
    [Child] private Label InfoLabel { get; set; }
    
    [Child] private ScrollContainer ChatScrollContainer { get; set; }
    [Child] private Label ChatLabel { get; set; }
    [Child] private LineEdit ChatLineEdit { get; set; }
    [Child] private Button ChatSendButton { get; set; }
    
    [Child] private Button Test1Button { get; set; }
    [Child] private Button Test2Button { get; set; }
    [Child] private Button Test3Button { get; set; }
    [Child] private Button LogButton { get; set; }
    [Child] private Button SaveButton { get; set; }
    [Child] private Button ExitButton { get; set; }
    [Child] private LineEdit SaveLineEdit { get; set; }
    
    private World.World.IReader _reader;
    private World.World.ICommandSender _commands;
    private PlayerQuery _players;
    private LocalPlayerPresentation _localPlayer;
    [Logger] private ILogger _log;
    
    public Hud InitPreReady(World.World.IReader reader, World.World.ICommandSender commands)
    {
        Di.Process(this);
        
        if (reader == null) _log.Error("Reader must be not null");
        if (commands == null) _log.Error("Command sender must be not null");
        _reader = reader;
        _commands = commands;
        _players = reader.Get<PlayerQuery>();
        _localPlayer = reader.Get<LocalPlayerPresentation>();
        
        return this;
    }

    public override void _Ready()
    {
        Di.Process(this);
        
        // The scroll bar learns the new height only after the label's layout, so the scroll follows its change
        ScrollBar chatScrollBar = ChatScrollContainer.GetVScrollBar();
        chatScrollBar.Changed += () => ChatScrollContainer.ScrollVertical = (int) chatScrollBar.MaxValue;
        ChatSendButton.Pressed += SendChat;
        ChatLineEdit.TextSubmitted += _ => SendChat();
        LogButton.Pressed += () => { Services.NodeTree.LogFullTree(GetTree().Root); };
        ExitButton.Pressed += () => { Services.MainScene.StartMainMenu(); };
        SaveButton.Pressed += () => _commands.Send(new SaveCommand(SaveLineEdit.Text));
    }

    public override void _Process(double delta)
    {
        InfoLabel.Text = "Players:\n"
                         + string.Join("\n", _players.OnlinePlayers().Select(player => player.Nick));

        // The server drops a save from anyone else without a reply
        bool isAdmin = _localPlayer.TryGetPlayer()?.IsAdmin == true;
        SaveButton.Visible = isAdmin;
        SaveLineEdit.Visible = isAdmin;

        if (_reader.Get<HudMailbox>().Read<ChatEntryAddedNotice>().Count == 0) return;

        ChatLabel.Text = string.Join("\n",
            _reader.Get<ChatPresentation>().Entries.Select(entry => FormatChatLine(entry, key => Tr(key))));
    }

    private void SendChat()
    {
        string text = ChatLineEdit.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        _commands.Send(new SendChatMessageCommand(text));
        ChatLineEdit.Clear();
    }

    public static string FormatChatLine(ChatPresentation.ChatEntry entry, Func<string, string> tr) =>
        entry switch
        {
            ChatPresentation.PlayerMessageEntry message => $"[{message.SenderNick}]: {message.Text}",
            ChatPresentation.ServerTextEntry message => $"[{tr("HUD__CHAT_SERVER_NICK")}]: {message.Text}",
            ChatPresentation.PlayerJoinedEntry joined => $"{joined.Nick} {tr("HUD__CHAT_PLAYER_JOINED")}",
            ChatPresentation.PlayerLeftEntry left => $"{left.Nick} {tr("HUD__CHAT_PLAYER_LEFT")}",
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
        };
}

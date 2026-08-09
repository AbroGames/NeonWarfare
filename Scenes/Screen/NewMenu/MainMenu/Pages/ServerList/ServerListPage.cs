using System;
using System.Globalization;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scripts.Service.KnownServers;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.ServerList;

public partial class ServerListPage : MainMenuPage
{
    // Known-servers list
    [Child] public VBoxContainer ServersListContainer { get; private set; }
    [Child] public Button RemoveServerButton { get; private set; }

    // Inline add form
    [Child] public LineEdit AddHostLineEdit { get; private set; }
    [Child] public SpinBox AddPortSpinBox { get; private set; }
    [Child] public LineEdit AddLabelLineEdit { get; private set; }
    [Child] public Button AddServerButton { get; private set; }

    // Direct connect
    [Child] public LineEdit DirectHostLineEdit { get; private set; }
    [Child] public Button ConnectButton { get; private set; }
    [Child] public Button BackButton { get; private set; }

    private KnownServer _selectedServer;

    public override void _Ready()
    {
        Di.Process(this);

        AddServerButton.Pressed += OnAddServer;
        RemoveServerButton.Pressed += OnRemoveServer;
        ConnectButton.Pressed += OnConnectDirect;
        BackButton.Pressed += () => GoBack();

        AddPortSpinBox.Value = Consts.DefaultPort;
        AddPortSpinBox.MaxValue = 65535;
        AddPortSpinBox.MinValue = 1;

        RemoveServerButton.Disabled = true;
        _selectedServer = null;

        PopulateServersList();
    }

    private void PopulateServersList()
    {
        foreach (var child in ServersListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var server in Services.KnownServers.GetAll())
        {
            var button = new Button();
            button.Text = String.IsNullOrEmpty(server.Label)
                ? $"{server.Host}:{server.Port?.ToString() ?? ""}"
                : $"{server.Label} ({server.Host}:{server.Port?.ToString() ?? ""})";
            button.Pressed += () =>
            {
                _selectedServer = server;
                RemoveServerButton.Disabled = false;
            };
            if (_selectedServer is not null && _selectedServer == server)
            {
                button.ButtonPressed = true;
            }
            ServersListContainer.AddChild(button);
        }
    }

    private void OnAddServer()
    {
        string host = AddHostLineEdit.Text?.Trim();
        if (String.IsNullOrWhiteSpace(host))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
            return;
        }

        int? port = (int) AddPortSpinBox.Value;
        string label = AddLabelLineEdit.Text?.Trim() ?? String.Empty;

        if (Services.KnownServers.Exists(host, port))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__ALREADY_EXISTS_ERROR")));
            return;
        }

        Services.KnownServers.Add(new KnownServer(host, port, label));

        AddHostLineEdit.Text = String.Empty;
        AddLabelLineEdit.Text = String.Empty;
        AddPortSpinBox.Value = Consts.DefaultPort;

        PopulateServersList();
    }

    private void OnRemoveServer()
    {
        if (_selectedServer is null)
        {
            return;
        }

        Services.KnownServers.Remove(_selectedServer);
        _selectedServer = null;
        RemoveServerButton.Disabled = true;
        PopulateServersList();
    }

    private void OnConnectDirect()
    {
        string raw = DirectHostLineEdit.Text?.Trim();
        if (String.IsNullOrWhiteSpace(raw))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
            return;
        }

        string host;
        int? port = null;
        int colon = raw.LastIndexOf(':');
        if (colon > 0 && int.TryParse(raw[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPort))
        {
            host = raw[..colon].Trim();
            port = parsedPort;
        }
        else
        {
            host = raw;
        }

        if (String.IsNullOrWhiteSpace(host))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
            return;
        }

        // Gate first; auto-add to known servers only when the game actually starts
        // (a cancelled gate must not mutate the list).
        TryStartGame(() =>
        {
            if (!Services.KnownServers.Exists(host, port))
            {
                Services.KnownServers.Add(new KnownServer(host, port, String.Empty));
            }
            Services.MainScene.ConnectToMultiplayerGame(host, port);
        });
    }
}

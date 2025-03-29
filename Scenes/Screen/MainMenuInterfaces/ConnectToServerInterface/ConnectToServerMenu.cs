using Godot;
using System;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces.ConnectToServerInterface;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.PersistenceService;

public partial class ConnectToServerMenu : Control
{
    private const string ServersStorageFilePath = "user://ServersStorage.json";
    [Export] [NotNull] private LineEdit _serverConnectionLineEdit { get; set; }
    [Export] [NotNull] private Button _connectToServerButton { get; set; }
    private ServersStorage _serversStorage;
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        _serversStorage = Persistence.Load(ServersStorageFilePath, new ServersStorage(), saveDefault: true);
        _serverConnectionLineEdit.Text = _serversStorage.LastConnectionString;

        _connectToServerButton.Pressed += () =>
        {
            _serversStorage.LastConnectionString = _serverConnectionLineEdit.Text;
            _serversStorage.AddServer(_serverConnectionLineEdit.Text);
            
            Persistence.Save(ServersStorageFilePath, _serversStorage);
        };
    }
}

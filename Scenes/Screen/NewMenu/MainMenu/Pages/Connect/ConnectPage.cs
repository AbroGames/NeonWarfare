using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Connect;

public partial class ConnectPage : MainMenuPage
{
    [Child] public LineEdit HostLineEdit { get; private set; }
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public Button ConnectToServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        //TODO: Сильные сомнения, что так корректно использовать. У нас может быть порт от режима создания сервера,
        // а хоста не будет, потому что ты никогда никуда не подключался. Я бы просто дефолтные значения оставлял во всех менюшках.
        // Зеленая кнопка и так уже есть, этого достаточно, остальное мне кажется будет мешать чаще. Если юзер не нажал на зеленую кнопку, а зашел в меню конретное,
        // то видимо у него не стандартный кейс.
        // Было:
        // HostLineEdit.Text = Services.GameSettings.GetSettings().LastGame.Host ?? String.Empty;
        // PortSpinBox.Value = Services.GameSettings.GetSettings().LastGame.Port ?? Consts.DefaultPort;
        HostLineEdit.Text = String.Empty;
        PortSpinBox.Value = Consts.DefaultPort;
        
        ConnectToServerButton.Pressed += ParseAndConnectToServer;
        CancelButton.Pressed += () => GoBack();
    }

    private void ParseAndConnectToServer()
    {
        
        string host = String.IsNullOrWhiteSpace(HostLineEdit.Text) ? null : HostLineEdit.Text.Trim();
        if (host is null)
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("CONNECT_MENU__HOSTNAME_UNSPECIFIED_ERROR")));
        }
        
        int port = (int) PortSpinBox.Value;
        Services.MainScene.ConnectToMultiplayerGame(host, port);
    }
}
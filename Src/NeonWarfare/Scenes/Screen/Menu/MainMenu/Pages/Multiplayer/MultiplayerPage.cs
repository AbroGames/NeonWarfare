using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.Multiplayer;

public partial class MultiplayerPage : MainMenuPage
{
    [Child] public Button CreateNewServerButton { get; private set; }
    [Child] public Button CreateFromSaveButton { get; private set; }
    [Child] public Button ConnectButton { get; private set; }
    [Child] public Button BackButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);

        CreateNewServerButton.Pressed += ()
            => GoNext(PagesProvider.PreparePage(PagesProvider.CreateNewServerPageScene));
        CreateFromSaveButton.Pressed += ()
            => GoNext(PagesProvider.PreparePage(PagesProvider.CreateSavedServerPageScene));
        ConnectButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ServerListPageScene));
        BackButton.Pressed += () => GoBack();
    }
}

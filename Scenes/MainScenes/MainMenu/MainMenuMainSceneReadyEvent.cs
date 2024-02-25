using KludgeBox.Events;

namespace AbroDraft.Scenes.MainScenes.MainMenu;

public readonly struct MainMenuMainSceneReadyEvent(MainMenuMainScene mainMenuMainScene) : IEvent
{
    public MainMenuMainScene MainMenuMainScene { get; } = mainMenuMainScene;
}
using KludgeBox.Events;

namespace AbroDraft;

public readonly struct MainMenuMainSceneReadyEvent(MainMenuMainScene mainMenuMainScene) : IEvent
{
    public MainMenuMainScene MainMenuMainScene { get; } = mainMenuMainScene;
}
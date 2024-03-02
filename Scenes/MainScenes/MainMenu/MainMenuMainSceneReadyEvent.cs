using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly struct MainMenuMainSceneReadyEvent(MainMenuMainScene mainMenuMainScene) : IEvent
{
    public MainMenuMainScene MainMenuMainScene { get; } = mainMenuMainScene;
}
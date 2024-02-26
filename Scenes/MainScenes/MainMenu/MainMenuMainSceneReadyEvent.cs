using KludgeBox.Events;

namespace NeoVector;

public readonly struct MainMenuMainSceneReadyEvent(MainMenuMainScene mainMenuMainScene) : IEvent
{
    public MainMenuMainScene MainMenuMainScene { get; } = mainMenuMainScene;
}
using KludgeBox.Events;

public readonly struct MainMenuMainSceneReadyEvent(MainMenuMainScene mainMenuMainScene) : IEvent
{
    public MainMenuMainScene MainMenuMainScene { get; } = mainMenuMainScene;
}
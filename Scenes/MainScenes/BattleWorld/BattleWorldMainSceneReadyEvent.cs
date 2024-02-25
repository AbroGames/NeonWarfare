using KludgeBox.Events;

public readonly struct BattleWorldMainSceneReadyEvent(BattleWorldMainScene battleWorldMainScene) : IEvent
{
    public BattleWorldMainScene BattleWorldMainScene { get; } = battleWorldMainScene;
}
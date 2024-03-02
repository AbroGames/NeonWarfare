using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly struct BattleWorldMainSceneReadyEvent(BattleWorldMainScene battleWorldMainScene) : IEvent
{
    public BattleWorldMainScene BattleWorldMainScene { get; } = battleWorldMainScene;
}
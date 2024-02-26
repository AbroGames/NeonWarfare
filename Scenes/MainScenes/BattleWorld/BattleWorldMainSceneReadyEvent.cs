using KludgeBox.Events;

namespace NeoVector;

public readonly struct BattleWorldMainSceneReadyEvent(BattleWorldMainScene battleWorldMainScene) : IEvent
{
    public BattleWorldMainScene BattleWorldMainScene { get; } = battleWorldMainScene;
}
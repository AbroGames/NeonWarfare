using KludgeBox.Events;

namespace AbroDraft;

public readonly struct BattleWorldMainSceneReadyEvent(BattleWorldMainScene battleWorldMainScene) : IEvent
{
    public BattleWorldMainScene BattleWorldMainScene { get; } = battleWorldMainScene;
}
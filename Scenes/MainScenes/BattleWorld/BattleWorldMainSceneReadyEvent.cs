using KludgeBox.Events;

namespace AbroDraft.Scenes.MainScenes.BattleWorld;

public readonly struct BattleWorldMainSceneReadyEvent(BattleWorldMainScene battleWorldMainScene) : IEvent
{
    public BattleWorldMainScene BattleWorldMainScene { get; } = battleWorldMainScene;
}
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Events;

namespace NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;

public partial class ClientBattleWorld
{
    [EventListener(ListenerSide.Client)]
    public void OnWaveStartedPacket(SC_WaveStartedPacket waveStartedPacket)
    {
        CurrentWave = waveStartedPacket.Number;
        if (CurrentWave >= AchievementsPrerequisites.AdvancedSurvival_WavesCount)
        {
            ClientRoot.Instance.UnlockAchievement(AchievementIds.AdvancedSurvivalAchievement);
        }
    }

    [EventListener(ListenerSide.Client)]
    public void OnWaveTimeSyncPacket(SC_WaveTimeSyncPacket waveTimePacket)
    {
        TimeToWave = waveTimePacket.SecondsToWave;
    }
}
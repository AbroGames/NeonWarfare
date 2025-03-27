using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;

public partial class ClientBattleWorld
{
    [GamePacket]
    public class SC_WaveStartedPacket(int number) : BinaryPacket
    {
        public int Number = number;
    }
    
    [GamePacket]
    public class SC_WaveTimeSyncPacket(double secondsToWave) : BinaryPacket
    {
        public double SecondsToWave = secondsToWave;
    }
}
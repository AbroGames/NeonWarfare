using Godot;
using KludgeBox.Godot.Services;

namespace NeonWarfare.Scripts;

public static class Consts
{
    
    public static class Global
    {
        public const int BroadcastId = (int) MultiplayerPeer.TargetPeerBroadcast;
        public const int ServerId = (int) MultiplayerPeer.TargetPeerServer;
    }
    
    public enum TransferChannel
    {
        Chat,
        StatsHp,
        StatsCache
    }
    
    public const string DefaultHost = "127.0.0.1";
    public const int DefaultPort = 25566;
    
    public static readonly AutoScalingService.AutoScalingSettings AutoScalingSettings = new(
        SmallestScaleFactor: 0.75f,
        ScaleOptions: [
            new(600, 0.875f),
            new(800, 1f),
            new(1200, 1.25f),
            new(1500, 1.5f)
        ]);
}
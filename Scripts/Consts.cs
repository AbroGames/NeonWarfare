using Godot;

namespace NeonWarfare.Scripts;

public static class Consts
{
    public const string DefaultPlayerName = "Player";
    public const string Localhost = "127.0.0.1";
    public const string DefaultHost = Localhost;
    public const int DefaultPort = 25566;
    
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
}
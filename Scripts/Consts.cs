using Godot;

namespace NeonWarfare.Scripts;

public static class Consts
{

    public static class Global
    {
        public const int BroadcastId = (int) MultiplayerPeer.TargetPeerBroadcast;
        public const int ServerId = (int) MultiplayerPeer.TargetPeerServer;
    }
}
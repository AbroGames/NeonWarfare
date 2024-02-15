using Godot;

namespace KludgeBox.Net;

public enum PacketSource
{
    Client,
    Server
}

public sealed record BinaryPacket (
    int SenderId,
    MultiplayerPeer.TransferModeEnum TransferMode,
    byte[] Data)
{
    public bool Processed { get; set; } = false;
    public PacketSource SenderType => SenderId == 1 ? PacketSource.Server : PacketSource.Client;


    public PacketReader StartReading()
    {
        return new PacketReader(this);
    }
}
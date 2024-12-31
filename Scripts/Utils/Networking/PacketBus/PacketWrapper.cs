namespace NeonWarfare.Utils.Networking;

public class PacketWrapper
{
    public bool IsValid => Packet is not null;
    public bool WasDelivered => DeliveryCount > 0;
    public bool WasErrored => ErrorCount > 0;
    public bool WasRejected => RejectionCount > 0;

    public int ErrorCount { get; private set; }
    public int DeliveryCount { get; private set; }
    public int RejectionCount { get; private set; }
    public IPacket Packet { get; private set; }

    public PacketWrapper(IPacket message)
    {
        Packet = message;
    }

    public void Accept()
    {
        DeliveryCount++;
    }

    public void Reject()
    {
        RejectionCount++;
    }

    public void Error()
    {
        ErrorCount++;
    }
}
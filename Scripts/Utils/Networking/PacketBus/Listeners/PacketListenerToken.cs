using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Utils.Networking;

public sealed class PacketListenerToken
{
    public ListenerHub Hub { get; }
    public IPacketListener Listener { get; }
    
    public PacketListenerToken(ListenerHub hub, IPacketListener listener)
    {
        Hub = hub;
        Listener = listener;
    }

    public void Unsubscribe()
    {
        Listener.Cancel();
        Hub.Unsubscribe(this);
    }
}
using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerTypes.Factory;

public interface IPacketListenerFactory
{
    /// <summary>
    /// Returns true if this listener factory can create listener from provided source
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    bool IsSourceAcceptable(object source);
    
    /// <summary>
    /// Creates new IListener from provided source
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    IPacketListener CreateDestination(object source);
}

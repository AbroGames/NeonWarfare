using System;
using System.Collections.Generic;
using NeonWarfare.Scripts.Utils.InstanceRouting;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerSources;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerTypes;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerTypes.Factory;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.PacketTypes;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus;

public class PacketBus
{
    public InstanceRouter InstanceRouter { get; }
    public IPacketListenerFactory PacketListenerFactory { get; }
    private Dictionary<Type, ListenerHub> _hubs { get; } = new();

    public PacketBus(IPacketListenerFactory packetListenerFactory = null, InstanceRouter instanceRouter = null)
    {
        InstanceRouter = instanceRouter ?? new InstanceRouter();
        PacketListenerFactory = packetListenerFactory ?? 
                                   new CombinedPacketListenerFactory(
                                       new StaticMethodPacketListenerFactory(), 
                                       new InstanceMethodPacketListenerFactory(InstanceRouter)
                                       );
    }

    public PacketWrapper AcceptPacket(IPacket packet)
    {
        if (packet is null)
            return null;
        
        var wrapper = new PacketWrapper(packet);
        var hub = GetDestinationHub(packet.GetType());
        hub.Accept(wrapper);
        
        return wrapper;
    }
    
    public PacketListenerToken Subscribe(IPacketListener listener)
    {
        var hub = GetDestinationHub(listener.PacketType);
        hub.Subscribe(listener);

        return hub.Subscribe(listener);
    }
    
    public PacketListenerToken Subscribe(object source)
    {
        if (!PacketListenerFactory.IsSourceAcceptable(source))
            throw new ArgumentException($"Source of type {source.GetType()} is not acceptable in this message bus");
            
        var listener = PacketListenerFactory.CreateDestination(source);
        var token = Subscribe(listener);
        
        return token;
    }
    
    public PacketListenerToken[] SubscribeAll(IListenerSource source)
    {
        var listenerSources = source.GetDestinations();
        List<PacketListenerToken> tokens = new();
        foreach (var listenerSource in listenerSources)
        {
            try
            {
                var token = Subscribe(listenerSource);
                tokens.Add(token);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        return tokens.ToArray();
    }

    private ListenerHub GetDestinationHub(Type packetType)
    {
        if (_hubs.TryGetValue(packetType, out var hub))
        {
            return hub;
        }
        
        var newHub = new ListenerHub(packetType);
        _hubs[packetType] = newHub;
        
        return newHub;
    }
}

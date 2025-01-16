using System;
using System.Collections.Generic;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerTypes;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus;

public class ListenerHub
{
    public Type PacketType { get; }
    public List<IPacketListener> Listeners { get; } = new List<IPacketListener>();
    public int DeliversBeforeCleanup { get; set; }
    
    private static int _hubsCount = 0;
    private int _hubId;
    private int _deliversCount;
    
    public ListenerHub(Type packetType, int deliversBeforeCleanup = 10)
    {
        PacketType = packetType;
        _hubId = _hubsCount++;
    }
    
    public void Accept(PacketWrapper packet)
    {
        if (packet.Packet.GetType() != PacketType)
        {
            packet.Reject();
            return;
        }
        
        packet.Accept();
        TryCleanup();
        foreach (var listener in Listeners)
        {
            try
            {
                listener.Deliver(packet.Packet);
            }
            catch (Exception)
            {
                packet.Error();
            }
        }
    }

    public PacketListenerToken Subscribe(IPacketListener listener)
    {
        Listeners.Add(listener);
        var token = new PacketListenerToken(this, listener);
        
        return token;
    }

    public void Unsubscribe(PacketListenerToken token)
    {
        Listeners.Remove(token.Listener);
    }

    public override string ToString()
    {
        return $"{GetType().Name}#{_hubId}";
    }

    private void TryCleanup()
    {
        if (DeliversBeforeCleanup <= 0)
        {
            return;
        }
        
        _deliversCount++;
        if (_deliversCount >= DeliversBeforeCleanup)
        {
            _deliversCount = 0;
            DoCleanup();
        }
    }

    private void DoCleanup()
    {
        Listeners.RemoveAll(destination => !destination.IsActive);
    }
}

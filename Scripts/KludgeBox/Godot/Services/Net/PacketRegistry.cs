using System;
using System.Collections.Generic;
using KludgeBox.Net.Packets;

namespace KludgeBox.Net;

public static class PacketRegistry
{
    public static event Action SynchronizationCompleted;
    public static bool IsSynchronized { get; set; } = true;
    
    private static int _nextId = 0;
    private static readonly Dictionary<int, Type> _packets = new();
    private static readonly Dictionary<Type, int> _packetIds = new();

    static PacketRegistry()
    {
        // This packet id must always be 0, so we add it in class constructor.
        ClearRegistry();
        RegisterPacketType(typeof(RegistrySynchronizationPacket));
    }

    public static void ScanPackets()
    {
        var packets = ReflectionExtensions.FindTypesWithAttributes(typeof(GamePacketAttribute));
        foreach (Type packet in packets)
        {
            RegisterPacketType(packet);
        }
    }
    
    
    public static int RegisterPacketType(Type type)
    {
        if (!type.IsAssignableTo(typeof(AbstractPacket))) throw new ArgumentException("Type is not packet");
        if (_packetIds.TryGetValue(type, out var packetId)) return packetId;

        var id = _nextId;
        _packets[id] = type;
        _packetIds[type] = id;
        _nextId++;
        Log.Debug($"Registered packet of type {type.FullName} with id {id}");
        return id;
    }

    public static Type GetPacketType(int id)
    {
        if (!IsSynchronized && id != 0) throw new InvalidOperationException("Packet registry must be synchronized with server first.");
        return _packets[id];
    }

    public static int GetPacketId(Type type)
    {
        return _packetIds[type];
    }

    public static RegistrySynchronizationPacket BuildSynchronizationPacket()
    {
        if (!KludgeBox.Net.NetworkOld.IsServer) throw new InvalidOperationException("Only server can send synchronization packets");

        var packet = new RegistrySynchronizationPacket();
        var types = packet.PacketTypeOrder;

        for (int i = 0; i < _nextId; i++)
        {
            types.Add(GetPacketType(i));
        }

        return packet;
    }

    public static void SynchronizeRegistry(RegistrySynchronizationPacket packet)
    {
        if (KludgeBox.Net.NetworkOld.BothLocal) return; // Already synchronized
        if (KludgeBox.Net.NetworkOld.IsServer) throw new InvalidOperationException("Only client can synchronize registry from packet");
        
        ClearRegistry();
        for (var i = 0; i < packet.PacketTypeOrder.Count; i++)
        {
            var type = packet.PacketTypeOrder[i];
            RegisterPacketType(type);
            Log.Info($"Registered {type} with id {i}");
        }

        IsSynchronized = true;
        SynchronizationCompleted?.Invoke();
    }

    private static void ClearRegistry()
    {
        _packets.Clear();
        _packetIds.Clear();
        _nextId = 0;
    }
}
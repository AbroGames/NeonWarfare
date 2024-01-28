using System;
using System.Collections.Generic;
using AbroDraft.Net.Packets;

namespace AbroDraft.Net;

public static class PacketRegistry
{
    private static int _nextId = 0;
    private static readonly Dictionary<int, Type> _packets = new();
    private static readonly Dictionary<Type, int> _packetIds = new();

    public static int RegisterPacketType(Type type)
    {
        if (!type.IsAssignableTo(typeof(AbstractPacket))) throw new ArgumentException("Type is not packet");
        if (_packetIds.TryGetValue(type, out var packetId)) return packetId;

        var id = _nextId;
        _packets[id] = type;
        _packetIds[type] = id;
        _nextId++;

        return id;
    }

    public static Type GetPacketType(int id)
    {
        return _packets[id];
    }

    public static int GetPacketId(Type type)
    {
        return _packetIds[type];
    }
}
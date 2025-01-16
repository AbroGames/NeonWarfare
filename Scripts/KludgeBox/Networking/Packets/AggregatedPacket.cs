using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KludgeBox.Collections;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Packets;

/// <summary>
/// This packet can contain multiple other packets.
/// </summary>
public sealed class AggregatedPacket : NetPacket
{
    public ReadOnlyHashSet<NetPacket> ContainedPackets => _packets.AsReadOnly();
    private HashSet<NetPacket> _packets = new();
    
    public void AddPacket(NetPacket packet)
    {
        if(packet is null)
            throw new ArgumentNullException(nameof(packet));
        
        if(packet == this)
            throw new ArgumentException("Packet cannot be added to itself");
        
        if(CheckForRecursions(this))
            throw new InvalidOperationException("Recursion detected");
        
        _packets.Add(packet);
    }

    /// <inheritdoc />
    public override byte[] ToBuffer(PacketRegistry packetRegistry)
    {
        using MemoryStream stream = new MemoryStream();
        foreach (var packet in _packets)
        {
            byte[] buffer = PacketHelper.EncodePacket(packet, packetRegistry);
            int length = buffer.Length;
            byte[] lengthBytes = BitConverter.GetBytes(length);

            stream.Write(lengthBytes, 0, lengthBytes.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        return stream.ToArray();
    }
    
    /// <inheritdoc />
    public override AggregatedPacket FromBuffer(byte[] compoundBuffer, PacketRegistry packetRegistry)
    {
        byte[][] buffers = SplitToBuffers(compoundBuffer);

        foreach (byte[] buffer in buffers)
        {
            var packet = PacketHelper.DecodePacket(buffer, packetRegistry);
            AddPacket(packet);
        }

        return this;
    }

    private byte[][] SplitToBuffers(byte[] compoundBuffer)
    {
        List<byte[]> buffers = new List<byte[]>();

        using MemoryStream stream = new MemoryStream(compoundBuffer);
        using BinaryReader reader = new BinaryReader(stream);
        
        while (stream.Position < stream.Length)
        {
            int length = reader.ReadInt32();
            byte[] buffer = reader.ReadBytes(length);
            buffers.Add(buffer);
        }

        return buffers.ToArray();
    }

    // Compound packets can contain another compound packets. Make sure that there is no recursion.
    private static bool CheckForRecursions(AggregatedPacket packet)
    {
        HashSet<AggregatedPacket> visited = new HashSet<AggregatedPacket>();
        return CheckForRecursionsInternal(packet, visited);
    }

    private static bool CheckForRecursionsInternal(AggregatedPacket packet, HashSet<AggregatedPacket> visited)
    {
        if (!visited.Add(packet))
            return true;

        if (packet._packets.OfType<AggregatedPacket>().Any(innerPacket => CheckForRecursionsInternal(innerPacket, visited)))
        {
            return true;
        }

        visited.Remove(packet);
        return false;
    }
}

using System.IO;

namespace KludgeBox.Networking;

public static class PacketHelper
{
    /// <summary>
    /// Reads packet type from first 4 bytes and passes the rest to deserialization.
    /// </summary>
    /// <param name="packet">Full packet with type ID prefix</param>
    /// <returns></returns>
    public static NetPacket DecodePacket(byte[] packet)
    {
        // transform byte array to stream and prepare reader for it
        using var stream = new MemoryStream(packet);
        using var reader = new BinaryReader(stream);
        
        // first 4 bytes are packet type ID
        var typeId = reader.ReadInt32();
        // the rest is packet data
        var packetData = reader.ReadBytes(packet.Length - 4);
        // get type from read ID
        var packetType = Netplay.PacketRegistry.GetType(typeId);
        // deserialize packet
        var packetObj = NetPacket.FromBuffer(packetType, packetData);
        
        return packetObj;
    }
    
    /// <summary>
    /// Encodes packet into a byte array with 4 byte type ID prefix.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    public static byte[] EncodePacket(NetPacket packet)
    {
        // get serialized packet data
        var packetData = packet.ToBuffer();
        // prepare stream and writer
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        // write 4 byte type ID prefix
        writer.Write(Netplay.PacketRegistry.GetTypeId(packet.GetType()));
        // write actual packet data
        writer.Write(packetData);
        // transform stream to byte array
        var bytes = stream.ToArray();

        return bytes;
    }
}
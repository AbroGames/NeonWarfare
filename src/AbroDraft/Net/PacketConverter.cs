using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AbroDraft.Net.Packets;
using Newtonsoft.Json;

namespace AbroDraft.Net;

public static class PacketConverter
{
    private static Regex _packetIdRegex = new Regex(@"^\d?::(?=.*)");
    private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        Formatting = Formatting.None
    };
    
    public static string Serialize(AbstractPacket packet)
    {
        var packetType = packet.GetType();
        var packetId = PacketRegistry.GetPacketId(packetType);
        var data = JsonConvert.SerializeObject(packet, packetType, _settings);
        
        var sb = new StringBuilder();

        sb.Append(packetId);
        sb.Append("::");
        sb.Append(data);
        
        return sb.ToString();
    }

    public static AbstractPacket Deserialize(string data)
    {
        var packetId = GetId(data);
        var packetType = PacketRegistry.GetPacketType(packetId);
        var cleanData = _packetIdRegex.Replace(data, "");

        return (AbstractPacket) JsonConvert.DeserializeObject(cleanData, packetType);
    }

    private static int GetId(string packetData)
    {
        return Int32.Parse(packetData.Substring(0, packetData.IndexOf("::", StringComparison.Ordinal)), NumberStyles.Any, CultureInfo.InvariantCulture);
    }
}
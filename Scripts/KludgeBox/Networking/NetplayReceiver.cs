using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;

namespace KludgeBox.Networking;

public static partial class Netplay
{
    internal static void OnPacketReceived(long id, byte[] packet)
    {
        var packetObj = PacketHelper.DecodePacket(packet);
        packetObj.SenderId = id;
        //Log.Info($"Received packet from {id} with {packet.Length} - 4 bytes. Received type is {packetObj.GetType().FullName}");
        
        EventBus.Publish(packetObj);
    }
}
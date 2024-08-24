using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using KludgeBox.Scheduling;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare;

[GamePacket]
public class ClientPingPacket(long pingId) : BinaryPacket
{
    public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
    
    public long PingId = pingId; //TODO Возможно, передавать в пакете резы предыдущего пинга. Чтобы у сервера тоже была инфа. Считать PacketLoss по пакетам пинга?
}

public partial class PingChecker : Node
{
    private Cooldown _pingSendCooldown = new(0.05); //TODO поменять на 0.5-1
    private IDictionary<long, Stopwatch> _pingIdToSentTime = new Dictionary<long, Stopwatch>(); //TODO очищать периодически
    private long _nextPingId = 0;
    
    public override void _Ready()
    {
        _pingSendCooldown.Ready += SendPingPacket;
    }    
    
    public override void _Process(double delta)
    {
        _pingSendCooldown.Update(delta);
    }
    
    private void SendPingPacket()
    {
        long pingId = _nextPingId++;
        Stopwatch stopwatch = new();
        _pingIdToSentTime.Add(pingId, stopwatch);
        
        stopwatch.Start();
        Network.SendToServer(new ClientPingPacket(pingId));
    }

    private void ReceivePingPacket(ServerPingPacket serverPingPacket)
    {
        long pingTime = _pingIdToSentTime[serverPingPacket.PingId].ElapsedMilliseconds;  //TODO сохранить куда-то? Передать в класс аналитики и сохранить инфу там? Считать среднее и перцентили
        Log.Debug("Ping: " + pingTime);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void StaticReceivePingPacket(ServerPingPacket serverPingPacket)
    {
        ClientRoot.Instance.Game.PingChecker.ReceivePingPacket(serverPingPacket);
    }
}
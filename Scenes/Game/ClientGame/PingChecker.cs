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
    
    public long PingId = pingId;
}

public partial class PingChecker : Node
{
    public record PingInfo(long PingId, Stopwatch SentTimer);
    
    private const double PingCooldown = 0.5;
    private const int MaxPingTimeout = 1000;

    public PingAnalyzer PingAnalyzer { get; private set; } = new();
    
    private Cooldown _pingSendCooldown = new(PingCooldown);
    private IDictionary<long, Stopwatch> _pingIdToSentTime = new Dictionary<long, Stopwatch>(); //Мапа для быстрого доступа к Stopwatch (хранит ссылку на таймер из _orderedPingInfo.SentTimer)
    private LinkedList<PingInfo> _orderedPingInfo = new(); //Список информации о пакетах пинга, отсортированы по PingId (т.е. в порядке отправки)
    private ISet<long> _successPingIdInCollections = new HashSet<long>(); //Список успешных ответов на пинги (содержит PingId). Отдельно от _orderedPingInfo, чтобы не делать каждый раз поиск элемента в LinkedList.
    private long _nextPingId = 0; //Следующее уникальное значение для пакета пинга
    private long _numberOfSuccessPackets = 0; //Количество полученных ответных пакетов пинга (отправлены более чем MaxPingTimeout миллисекунд назад)
    private long _numberOfLossesPackets = 0; //Количество потерянных пакетов пинга (отправлены более чем MaxPingTimeout миллисекунд назад)
    
    public void Start()
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
        _orderedPingInfo.AddLast(new PingInfo(pingId, stopwatch));
        
        stopwatch.Start();
        Network.SendToServer(new ClientPingPacket(pingId));
        
        CheckAndDeleteOldAttempts();
    }

    private void CheckAndDeleteOldAttempts()
    {
        var currentElement = _orderedPingInfo.First;
        while (currentElement != null && currentElement.Value.SentTimer.ElapsedMilliseconds > MaxPingTimeout)
        {
            long pingId = currentElement.Value.PingId;
            if (_successPingIdInCollections.Contains(pingId))
            {
                _numberOfSuccessPackets++;
            }
            else
            {
                _numberOfLossesPackets++;
            }
            
            _pingIdToSentTime.Remove(pingId);
            _orderedPingInfo.Remove(currentElement);
            _successPingIdInCollections.Remove(pingId);
            currentElement = currentElement.Next;
        }
    }
    
    [EventListener(ListenerSide.Client)]
    public void ReceivePingPacket(ServerPingPacket serverPingPacket)
    {
        if (!_pingIdToSentTime.ContainsKey(serverPingPacket.PingId)) //Сообщение уже было посчитано как потерянное
        {
            return;
        }
        
        long pingTime = _pingIdToSentTime[serverPingPacket.PingId].ElapsedMilliseconds;
        _successPingIdInCollections.Add(serverPingPacket.PingId);

        CheckAndDeleteOldAttempts();
        PingAnalyzer.Analyze(pingTime, _numberOfSuccessPackets, _numberOfLossesPackets);
    }
}
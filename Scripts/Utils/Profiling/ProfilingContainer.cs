using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Root.ClientRoot;

namespace NeonWarfare.Scripts.Utils.Profiling;

public class ProfilingContainer
{
    public static ProfilingContainer Instance { get; private set; }
    public static bool IsRunning => Instance is not null;
    public static List<Type> GlobalProfilingEventTypes { get; } = [
        typeof(PerformanceProfilingEvent),
        typeof(IncomingNetworkProfilingEvent),
        typeof(OutgoingNetworkProfilingEvent),
        typeof(GameWorldProfilingEvent)
    ];

    public List<string> PacketTypes { get; private set; } = new();
    public List<string> ProfilingEventTypes { get; private set; } = new();
    
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public string GameSettings { get; private set; }
    public string SystemInfo { get; private set; }
    public long StopwatchFrequency { get; private set; }

    public List<ProfilingEvent> ProfilingEvents { get; private set; } = new();
    
    private Stopwatch _stopwatch;
    private System.Diagnostics.Process _process;
    private Stream _fileStream;

    private ProfilingContainer()
    {
        
    }

    public static void StartProfilingSession(string filePath)
    {
        Instance = new ProfilingContainer();
        
        Instance.StopwatchFrequency = Stopwatch.Frequency;
        Instance.StartTime = DateTime.Now;
        Instance._process = System.Diagnostics.Process.GetCurrentProcess();
        Instance._stopwatch = Stopwatch.StartNew();

        Instance._fileStream = File.OpenWrite(filePath);
        
        
    }

    public static void AddEvent(ProfilingEvent profilingEvent)
    {
        if (!IsRunning)
            return;
        
        AddEventLow(profilingEvent);
        AddPerformanceData();
    }

    private static void AddPerformanceData()
    {
        var perf = new PerformanceProfilingEvent(
            workingSet64: Instance._process.WorkingSet64,
            renderVideoMemUsed: (long)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed),
            nodesCount: (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
            orphanNodesCount: (int)Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
            resourcesCount: (int)Performance.GetMonitor(Performance.Monitor.ObjectResourceCount),
            fps: Performance.GetMonitor(Performance.Monitor.TimeFps),
            tps: Performance.GetMonitor(Performance.Monitor.TimeProcess)
            );
        
        AddEventLow(perf);
    }

    private static void AddEventLow(ProfilingEvent profilingEvent)
    {
        profilingEvent.Timestamp = Instance._stopwatch.ElapsedTicks;
        Instance.ProfilingEvents.Add(profilingEvent);
    }

    private void FLushProfilingData()
    {
        
    }

    public static ProfilingContainer StopProfilingSession()
    {
        if (!IsRunning)
            return null;
        
        var instance = Instance;
        Instance = null;
        instance._stopwatch.Stop();
        instance.EndTime = DateTime.Now;

        instance.PacketTypes = NetworkProfilingEvent.PacketTypes.Select(type => type.FullName).ToList();
        instance.ProfilingEventTypes = GlobalProfilingEventTypes.Select(type => type.FullName).ToList();

        var settings = ClientRoot.Instance.Settings;
        instance.GameSettings = $"Player: {settings.PlayerName} Color: {settings.PlayerColor.ToHtml()}\n" +
                                $"Audio volume: \n" +
                                $"  Master: {settings.MasterVolume}\n" +
                                $"  Sounds: {settings.SoundVolume}\n" +
                                $"  Music: {settings.MusicVolume}";
        
        var videoAdapterInfo = OS.GetVideoAdapterDriverInfo();
        instance.SystemInfo = $"OS: {OS.GetName()} Version: {OS.GetVersion()} Distro: {OS.GetDistributionName()}\n" +
                              $"Total RAM: {OS.GetMemoryInfo()["physical"]}" +
                              $"CPU: {OS.GetProcessorName()}\n" +
                              $"CPU Count: {OS.GetProcessorCount()}\n" +
                              $"GPU: {videoAdapterInfo[0]} {videoAdapterInfo[1]}\n" +
                              $"Cmd args: {String.Join(' ', OS.GetCmdlineUserArgs())}";

        return instance;
    }

    private const string PacketTypesSectionMark = "=== PACKET TYPES SECTION ===";
    private const string ProfilingEventTypesSectionMark = "=== PROFILING EVENT TYPES SECTION ===";
    private const string ProfilingEventSectionMark = "=== PROFILING EVENT ===";
    
    public void WriteProfilingSession(Stream stream)
    {
        var writer = new BinaryWriter(stream);
        
        writer.Write(GameSettings);
        writer.Write(SystemInfo);
        writer.Write(StopwatchFrequency);
        writer.Write(StartTime.ToBinary());
        writer.Write(EndTime.ToBinary());

        writer.Write(PacketTypesSectionMark);
        foreach (var packetType in PacketTypes)
        {
            writer.Write(packetType);
        }
        
        writer.Write(ProfilingEventTypesSectionMark);
        foreach (var profilingEventType in ProfilingEventTypes)
        {
            writer.Write(profilingEventType);
        }

        writer.Write(ProfilingEventSectionMark);
        foreach (var evt in ProfilingEvents)
        {
            writer.Write(ProfilingEventTypes.IndexOf(evt.GetType().FullName));
            writer.Write(evt.Timestamp);
            evt.SerializeData(writer);
        }
    }

    public void ReadProfilingSession(Stream stream)
    {
        var reader = new BinaryReader(stream);
        
        GameSettings = reader.ReadString();
        SystemInfo = reader.ReadString();
        StopwatchFrequency = reader.ReadInt64();
        StartTime = DateTime.FromBinary(reader.ReadInt64());
        EndTime = DateTime.FromBinary(reader.ReadInt64());
        
        var section = reader.ReadString();
        while (section is not null)
        {
            section = ReadSection(reader, section);
        }
    }

    private string ReadSection(BinaryReader reader, string section)
    {
        
        return section;
    }

    /*private void ReadPacketTypesSection(BinaryReader reader)
    {
        string lastValue = null;
        bool hasValues = true;
        while (hasValues)
        {
            
        }
    }
    
    private void ReadPacketTypesSection(BinaryReader reader)
    {
        
    }*/
}
using System;
using System.Diagnostics;
using System.Text;
using Godot;
using KludgeBox.Core.Cooldown;

namespace NeonWarfare.Scenes.World.Service.Performance;

public partial class WorldSharpPerformance : Node
{

    public int TotalManagedMemoryMb { get; private set; }
    
    // Contain delta between last and current calling method UpdateMetrics
    public int GcCallsForGen0 { get; private set; }
    public int GcCallsForGen1 { get; private set; }
    public int GcCallsForGen2 { get; private set; }
    public int AllocatedMb { get; private set; }
    
    // Contain average for the last second
    public double ProcessorCoreUse { get; private set; }
    
    private int _lastSumGcCallsForGen0;
    private int _lastSumGcCallsForGen1;
    private int _lastSumGcCallsForGen2;
    private int _lastSumAllocatedMb;
    private double _lastSumTotalProcessorTime;
    
    private int _logicalProcessors;
    private Process _currentProcess;
    
    private const double UpdateMetricsInterval = 1.0;
    private AutoCooldown _cooldown;

    public override void _Ready()
    {
        _cooldown = new(UpdateMetricsInterval, true, UpdateMetrics);
        _logicalProcessors = System.Environment.ProcessorCount;
        _currentProcess = Process.GetCurrentProcess();
    }

    public override void _Process(double delta)
    {
        _cooldown.Update(delta);
    }
    
    public String GetManyLinesString()
    {
        StringBuilder sb = new();
        
        sb.Append($"Managed memory: {TotalManagedMemoryMb} mb\n");
        sb.Append($"Last second allocated memory: {AllocatedMb} mb\n");
        sb.Append($"Last second GC calls (gen 0/1/2): {GcCallsForGen0}/{GcCallsForGen1}/{GcCallsForGen2}\n");
        sb.Append($"Core use: {ProcessorCoreUse:N1}%\n");

        return sb.ToString();
    }
    
    public String GetTwoLinesString()
    {
        StringBuilder sb = new();
        
        sb.Append($"Managed memory: {TotalManagedMemoryMb} mb    ");
        sb.Append($"Last second allocated memory: {AllocatedMb} mb\n");
        sb.Append($"Core use: {ProcessorCoreUse:N1}%    ");
        sb.Append($"Last second GC calls (gen 0/1/2): {GcCallsForGen0}/{GcCallsForGen1}/{GcCallsForGen2}\n");

        return sb.ToString();
    }

    private void UpdateMetrics()
    {
        TotalManagedMemoryMb = (int) (GC.GetTotalMemory(false) / 1024 / 1024);
        
        int sumGcCallsForGen0 = GC.CollectionCount(0);
        GcCallsForGen0 = sumGcCallsForGen0 - _lastSumGcCallsForGen0;
        _lastSumGcCallsForGen0 = sumGcCallsForGen0;
        
        int sumGcCallsForGen1 = GC.CollectionCount(1);
        GcCallsForGen1 = sumGcCallsForGen1 - _lastSumGcCallsForGen1;
        _lastSumGcCallsForGen1 = sumGcCallsForGen1;
        
        int sumGcCallsForGen2 = GC.CollectionCount(2);
        GcCallsForGen2 = sumGcCallsForGen2 - _lastSumGcCallsForGen2;
        _lastSumGcCallsForGen2 = sumGcCallsForGen2;
        
        int sumAllocatedMb = (int) (GC.GetTotalAllocatedBytes() / 1024 / 1024);
        AllocatedMb = sumAllocatedMb - _lastSumAllocatedMb;
        _lastSumAllocatedMb = sumAllocatedMb;
        
        double sumTotalProcessorTime = _currentProcess.TotalProcessorTime.TotalMicroseconds;
        ProcessorCoreUse = (sumTotalProcessorTime - _lastSumTotalProcessorTime) * _logicalProcessors / 1000 / 1000;
        _lastSumTotalProcessorTime = sumTotalProcessorTime;
    }
}
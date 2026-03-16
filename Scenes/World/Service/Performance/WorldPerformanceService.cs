using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.World.Service.Performance;

public partial class WorldPerformanceService : Node
{
    [Child] public WorldGodotPerformance Godot;
    [Child] public WorldSharpPerformance Sharp;
    [Child] public WorldENetPerformance ENet;
    [Child] public WorldPingPerformance Ping;

    public override void _Ready()
    {
        Di.Process(this);
    }
}
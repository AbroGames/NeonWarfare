using GodotBox.Godot;
using NeonWarfare.Scenes.World;

namespace NeonWarfare.GameTests.World.Fixtures;

public class ManualFrameProvider(ulong frame = 1) : FrameProvider
{
    public ulong Frame { get; set; } = frame;

    public override ulong GetProcessFrames() => Frame;
}

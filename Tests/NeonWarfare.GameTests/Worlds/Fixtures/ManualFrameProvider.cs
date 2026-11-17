using GodotBox.Godot;
using NeonWarfare.Scenes.Worlds;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

public class ManualFrameProvider(ulong frame = 1) : FrameProvider
{
    public ulong Frame { get; set; } = frame;

    public override ulong GetProcessFrames() => Frame;
}

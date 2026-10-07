namespace GodotBox.Godot;

// What TimeProvider is for the clock: code that counts engine frames takes it, so a test can advance the frames by
// hand instead of waiting for the engine
public class FrameProvider
{
    public static FrameProvider Engine { get; } = new();

    protected FrameProvider() { }

    public virtual ulong GetProcessFrames() => global::Godot.Engine.GetProcessFrames();
}

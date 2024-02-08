public readonly struct CameraProcessEvent(Camera camera, double delta)
{
    public Camera Camera { get; } = camera;
    public double Delta { get; }  = delta;
}
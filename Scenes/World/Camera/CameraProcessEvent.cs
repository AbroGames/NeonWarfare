public class CameraProcessEvent(Camera camera, double delta)
{
    public Camera Camera { get; private set; } = camera;
    public double Delta { get; private set; } = delta;
}
public readonly struct CameraReadyEvent(Camera camera)
{
    public Camera Camera { get; } = camera;
}
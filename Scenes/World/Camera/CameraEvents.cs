public readonly record struct CameraProcessEvent(Camera Camera, double Delta);
public readonly record struct CameraDeferredProcessEvent(Camera Camera, double Delta);
public readonly record struct CameraReadyEvent(Camera Camera);
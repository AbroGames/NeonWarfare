using Godot;

[GameService]
public class CameraService
{
    private const double OveroptimizedValue = 1 / 1.1;
    public CameraService()
    {
        EventBus.Subscribe<CameraReadyEvent>(OnCameraReadyEvent);
        EventBus.Subscribe<CameraProcessEvent>(OnCameraProcessEvent);
        EventBus.Subscribe<CameraDeferredProcessEvent>(OnCameraDeferredProcessEvent);
        EventBus.Subscribe<PlayerMouseWheelInputEvent>(OnMouseWheel);
    }

    private void OnCameraDeferredProcessEvent(CameraDeferredProcessEvent deferredProcess)
    {
        
    }

    public void OnMouseWheel(PlayerMouseWheelInputEvent wheelEvent)
    {
        wheelEvent.Deconstruct(out var player, out var eventType);
        ProcessZoom(player.Camera, eventType);
    }
    public void OnCameraReadyEvent(CameraReadyEvent cameraReadyEvent)
    {
        InitCamera(cameraReadyEvent.Camera);
    }
    
    public void OnCameraProcessEvent(CameraProcessEvent cameraProcessEvent) 
    {
        MoveCamera(cameraProcessEvent.Camera, cameraProcessEvent.Delta);
        UpdateShifts(cameraProcessEvent.Camera, cameraProcessEvent.Delta);
    }

    public void UpdateShifts(Camera camera, double delta)
    {
        foreach (var punch in camera.Shifts)
        {
            punch.Update(delta);
        }

        camera.Shifts.RemoveAll(s => !s.IsAlive);
    }
    public void InitCamera(Camera camera)
    {
        camera.ActualPosition = camera.Position;
        camera.TargetPosition = camera.Position;
    }

    public void MoveCamera(Camera camera, double delta)
    {
        if (camera.TargetNode is null) return;
        
        camera.TargetPosition = camera.TargetNode.Position;
        var availableMovement = (camera.TargetPosition + camera.PositionShift) - camera.ActualPosition;
        var actualMovement = availableMovement * Mathf.Pow(camera.SmoothingBase, camera.SmoothingPower);
		
        camera.ActualPosition += actualMovement;
        camera.Position = camera.ActualPosition + camera.HardPositionShift + camera.AdditionalShift;
    }

    public void ProcessZoom(Camera camera, WheelEventType wheelEvent)
    {
        if (wheelEvent is WheelEventType.WheelUp)
        {
            camera.Zoom *= 1.1;
        }
        if (wheelEvent is WheelEventType.WheelDown)
        {
            camera.Zoom *= OveroptimizedValue;
        }
    }
}
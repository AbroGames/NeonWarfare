using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

[GameService]
public class CameraService
{

    [EventListener]
    public void OnCameraDeferredProcessEvent(CameraDeferredProcessEvent deferredProcess) { }

    [EventListener]
    public void OnMouseWheel(PlayerMouseWheelInputEvent wheelEvent)
    {
        var (player, eventType) = wheelEvent;
        Camera camera = player.Camera;
        
        if (eventType is WheelEventType.WheelUp)
        {
            camera.Zoom *= 1.1;
        }
        if (eventType is WheelEventType.WheelDown)
        {
            camera.Zoom *= 1 / 1.1;
        }
    }
    
    [EventListener]
    public void OnCameraReadyEvent(CameraReadyEvent cameraReadyEvent)
    {
        Camera camera = cameraReadyEvent.Camera;
        
        camera.ActualPosition = camera.Position;
        camera.TargetPosition = camera.Position;
    }
    
    [EventListener]
    public void OnCameraProcessEvent(CameraProcessEvent cameraProcessEvent) 
    {
        MoveCamera(cameraProcessEvent.Camera, cameraProcessEvent.Delta);
        UpdateShifts(cameraProcessEvent.Camera, cameraProcessEvent.Delta);
    }

    private void MoveCamera(Camera camera, double delta)
    {
        if (camera.TargetNode is null) return;
        
        camera.TargetPosition = camera.TargetNode.Position;
        var availableMovement = (camera.TargetPosition + camera.PositionShift) - camera.ActualPosition;
        var actualMovement = availableMovement * Mathf.Pow(camera.SmoothingBase, camera.SmoothingPower);
		
        camera.ActualPosition += actualMovement;
        camera.Position = camera.ActualPosition + camera.HardPositionShift + camera.AdditionalShift;
    }

    private void UpdateShifts(Camera camera, double delta)
    {
        foreach (var punch in camera.Shifts)
        {
            punch.Update(delta);
        }

        camera.Shifts.RemoveAll(s => !s.IsAlive);
    }
}
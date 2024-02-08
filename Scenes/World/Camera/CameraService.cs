using Godot;

public class CameraService
{
    
    public CameraService()
    {
        Root.Instance.EventBus.Subscribe<CameraReadyEvent>(OnCameraReadyEvent);
        Root.Instance.EventBus.Subscribe<CameraProcessEvent>(OnCameraProcessEvent);
    }
    
    public void OnCameraReadyEvent(CameraReadyEvent cameraReadyEvent)
    {
        InitCamera(cameraReadyEvent.Camera);
    }
    
    public void OnCameraProcessEvent(CameraProcessEvent cameraProcessEvent) 
    {
        MoveCamera(cameraProcessEvent.Camera, cameraProcessEvent.Delta);
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
        camera.Position = camera.ActualPosition + camera.HardPositionShift;
    }
}
using Godot;

public class PlayerCameraService
{
    
    public PlayerCameraService()
    {
        Root.Instance.EventBus.Subscribe<CameraReadyEvent>(OnCameraReadyEvent);
        Root.Instance.EventBus.Subscribe<CameraProcessEvent>(OnCameraProcessEvent);
    }
    
    public void OnCameraReadyEvent(CameraReadyEvent cameraReadyEvent)
    {
        initCamera(cameraReadyEvent.Camera);
    }
    
    public void OnCameraProcessEvent(CameraProcessEvent cameraProcessEvent) 
    {
        moveCamera(cameraProcessEvent.Camera, cameraProcessEvent.Delta);
    }

    public void initCamera(Camera camera)
    {
        camera.ActualPosition = camera.Position;
        camera.TargetPosition = camera.Position;
    }

    public void moveCamera(Camera camera, double delta)
    {
        if (camera.TargetNode is null) return;
        
        camera.TargetPosition = camera.TargetNode.Position;
        var availableMovement = (camera.TargetPosition + camera.PositionShift) - camera.ActualPosition;
        var actualMovement = availableMovement * Mathf.Pow(camera.SmoothingBase, camera.SmoothingPower);
		
        camera.ActualPosition += actualMovement;
        camera.Position = camera.ActualPosition + camera.HardPositionShift;
    }
}
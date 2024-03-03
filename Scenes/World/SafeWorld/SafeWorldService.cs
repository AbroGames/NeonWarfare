using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class SafeWorldService
{
    
    [EventListener]
    public void OnSafeWorldReadyEvent(SafeWorldReadyEvent safeWorldReadyEvent)
    {
        NeoVector.SafeWorld safeWorld = safeWorldReadyEvent.SafeWorld;
        
        safeWorld.Player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
        safeWorld.Player.Position = Vec(500, 500);
		
        var camera = new Camera(); //TODO to camera service
        camera.Position = safeWorld.Player.Position;
        camera.TargetNode = safeWorld.Player;
        camera.Zoom = Vec(0.65);
        camera.SmoothingPower = 1.5;
        safeWorld.AddChild(camera);
        camera.Enabled = true;

        var floor = safeWorld.Floor;
        floor.Camera = camera;
        floor.ForceCheck();
        
        safeWorld.AddChild(safeWorld.Player); // must be here to draw over the floor
        PlaySafeMusic(); //TODO to music service (safe music service)
    }
    
    [EventListener]
    public void OnSafeWorldProcessEvent(SafeWorldProcessEvent safeWorldProcessEvent) { }
    
    public void PlaySafeMusic()
    {
        var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
        music.Finished += PlaySafeMusic;
    }
}
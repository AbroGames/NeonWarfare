using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft.World;

[GameService]
public class SafeWorldService
{
    
    [GameEventListener]
    public void OnSafeWorldReadyEvent(SafeWorldReadyEvent safeWorldReadyEvent)
    {
        SafeWorld safeWorld = safeWorldReadyEvent.SafeWorld;
        
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
        
        safeWorld.AddChild(safeWorld.Player); // must be here to draw over the floor
        PlaySafeMusic(); //TODO to music service (safe music service)
    }
    
    [GameEventListener]
    public void OnSafeWorldProcessEvent(SafeWorldProcessEvent safeWorldProcessEvent) { }
    
    public void PlaySafeMusic()
    {
        var music = Audio2D.PlayMusic(Music.MainBgm, 0.75f);
        music.Finished += PlaySafeMusic;
    }
}
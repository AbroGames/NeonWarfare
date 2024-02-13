using Game.Content;
using Godot;

[GameService]
public class SafeWorldService
{
    
    public SafeWorldService()
    {
        EventBus.Subscribe<SafeWorldReadyEvent>(OnSafeWorldReadyEvent);
        EventBus.Subscribe<SafeWorldProcessEvent>(OnSafeWorldProcessEvent);
    }
    
    public void OnSafeWorldReadyEvent(SafeWorldReadyEvent safeWorldReadyEvent)
    {
        InitSafeWorld(safeWorldReadyEvent.SafeWorld);
    }
    
    public void OnSafeWorldProcessEvent(SafeWorldProcessEvent safeWorldProcessEvent)
    {
        
    }

    public void InitSafeWorld(SafeWorld safeWorld)
    {
        safeWorld.Player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
        safeWorld.Player.Position = Vec(500, 500);
		
        var camera = new Camera(); //TODO to camera service
        camera.Position = safeWorld.Player.Position;
        camera.TargetNode = safeWorld.Player;
        camera.Zoom = Vec(0.65);
        camera.SmoothingPower = 1.5;
        safeWorld.AddChild(camera);
        camera.Enabled = true;

        var floor = new Floor();
        floor.Camera = camera;
        safeWorld.AddChild(floor);
        
        safeWorld.AddChild(safeWorld.Player); // must be here to draw over the floor
        PlaySafeMusic(); //TODO to music service (safe music service)
    }
    
    public void PlaySafeMusic()
    {
        var music = Audio2D.PlayMusic(Music.MainBgm, 0.5f);
        music.Finished += PlaySafeMusic;
    }
}
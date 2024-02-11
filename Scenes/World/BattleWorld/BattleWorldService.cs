using Game.Content;
using Godot;
using KludgeBox;

public class BattleWorldService
{
    
    public BattleWorldService()
    {
        Root.Instance.EventBus.Subscribe<BattleWorldReadyEvent>(OnBattleWorldReadyEvent);
        Root.Instance.EventBus.Subscribe<BattleWorldProcessEvent>(OnBattleWorldProcessEvent);
    }
    
    public void OnBattleWorldReadyEvent(BattleWorldReadyEvent battleWorldReadyEvent)
    {
        InitBattleWorld(battleWorldReadyEvent.BattleWorld);
    }
    
    public void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldProcessEvent)
    {
        
    }

    public void InitBattleWorld(BattleWorld battleWorld)
    {
        battleWorld.Player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
        battleWorld.Player.Position = Vec(500, 500);
		
        var camera = new Camera(); //TODO to camera service
        camera.Position = battleWorld.Player.Position;
        camera.TargetNode = battleWorld.Player;
        camera.Zoom = Vec(0.65);
        camera.SmoothingPower = 1.5;
        battleWorld.AddChild(camera);
        camera.Enabled = true;

        var floor = new Floor();
        floor.Camera = camera;
        battleWorld.AddChild(floor);
		
        Node2D ally = Root.Instance.PackedScenes.World.Ally.Instantiate<Node2D>();
        ally.Position = Vec(600, 600);
        battleWorld.AddChild(ally);
        battleWorld.AddChild(battleWorld.Player); // must be here to draw over the floor
        
        if (Rand.Chance(0.5))//TODO to music service (battle music service)
        {
            PlayBattleMusic1();
        }
        else
        {
            PlayBattleMusic2();
        }
    }
    
    public void PlayBattleMusic1()
    {
        var music = Audio2D.PlayMusic(Music.WorldBgm1, 0.7f);
        music.Finished += PlayBattleMusic2;
    }
    
    public void PlayBattleMusic2()
    {
        var music = Audio2D.PlayMusic(Music.WorldBgm2, 0.7f);
        music.Finished += PlayBattleMusic1;
    }
}
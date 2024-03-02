using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector.World;

[GameService]
public class BattleWorldService
{
    
    [EventListener]
    public void OnBattleWorldReadyEvent(BattleWorldReadyEvent battleWorldReadyEvent)
    {
        BattleWorld battleWorld = battleWorldReadyEvent.BattleWorld;
        
        battleWorld.Player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
        battleWorld.Player.Position = Vec(500, 500);
		
        var camera = new Camera(); //TODO to camera service
        camera.Position = battleWorld.Player.Position;
        camera.TargetNode = battleWorld.Player;
        camera.Zoom = Vec(0.3);
        camera.SmoothingPower = 1.5;
        battleWorld.AddChild(camera);
        camera.Enabled = true;

        var floor = battleWorld.Floor;
        floor.Camera = camera;
        floor.ForceCheck();
		
        Node2D ally = Root.Instance.PackedScenes.World.Ally.Instantiate<Node2D>();
        ally.Position = Vec(600, 600);
        battleWorld.AddChild(ally);
        battleWorld.AddChild(battleWorld.Player); // must be here to draw over the floor
        
        //EventBus.Publish(new PlayerLevelUpEvent(battleWorld.Player, 20));
        
        if (Rand.Chance(0.5))//TODO to music service (battle music service)
        {
            PlayBattleMusic1();
        }
        else
        {
            PlayBattleMusic2();
        }
    }

    [EventListener]
    public void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldProcessEvent) { }
    
    public void PlayBattleMusic1()
    {
        var music = Audio2D.PlayMusic(Music.WorldBgm1, 0.5f);
        music.Finished += PlayBattleMusic2;
    }
    
    public void PlayBattleMusic2()
    {
        var music = Audio2D.PlayMusic(Music.WorldBgm2, 0.5f);
        music.Finished += PlayBattleMusic1;
    }
}
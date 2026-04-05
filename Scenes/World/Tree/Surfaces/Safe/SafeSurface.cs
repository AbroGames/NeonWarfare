using Godot;
using KludgeBox.DI.Requests.ParentInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;

namespace NeonWarfare.Scenes.World.Tree.Surfaces.Safe;

public partial class SafeSurface : Surface
{
    [Parent(true)] private World _world;
    
    public override void _Ready()
    {
        Di.Process(this);
    }

    public override void InitOnServer()
    {
        AddWall(300, 300);
        AddWall(350, 350);
        AddWall(400, 400).Scale = Vec2(1, 4); //TODO wall to sync scenes and to InitOnServer();
        
        //AddBotCharacter(250, 250, Vec2(1, 0));
        //AddBotCharacter(350, 250, Vec2(-1, 0));
        //AddBotCharacter(450, 250, Vec2(-1, 0));

        Character bot0 = _world.EnemyService.AddBotCharacter(700, 200);
        //bot0.LogId = "Bot0";
        
        Character bot1 = _world.EnemyService.AddBotCharacter(700, 250);
        bot1.Mass *= 5; //bot1.LogId = "Bot1";
        Character bot2 = _world.EnemyService.AddBotCharacter(700, 300);
        bot2.Mass /= 5;
        Character bot3 = _world.EnemyService.AddBotCharacter(700, 350);
        bot3.Controller.ForceCoef *= 5;
        Character bot4 = _world.EnemyService.AddBotCharacter(700, 400);
        bot4.Controller.ForceCoef /= 5;
        Character bot5 = _world.EnemyService.AddBotCharacter(700, 450);
        bot5.Mass *= 5; bot5.Controller.ForceCoef *= 5;
        Character bot6 = _world.EnemyService.AddBotCharacter(700, 500);
        bot6.Mass /= 5; bot6.Controller.ForceCoef /= 5;
        Character bot7 = _world.EnemyService.AddBotCharacter(700, 550);
        bot7.Mass *= 5; bot7.Controller.ForceCoef /= 5;
        Character bot8 = _world.EnemyService.AddBotCharacter(700, 600);
        bot8.Mass /= 5; bot8.Controller.ForceCoef *= 5;
    }
    
    public Node2D AddWall(float x, float y)
    {
        Node2D wall = _world.SyncedPackedScenes.Wall.Instantiate<Node2D>();
        wall.Position = Vec2(x, y);
        this.AddChildWithUniqueName(wall, "Wall");
        return wall;
    }
}
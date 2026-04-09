using Godot;
using NeonWarfare.Scenes.Entity.Character;

namespace NeonWarfare.Scenes.World.Tree.Surfaces.Safe;

public partial class SafeSurface : Surface
{

    public override void InitOnServer()
    {
        base.InitOnServer();
        
        AddWall(300, 300);
        AddWall(350, 350);
        AddWall(400, 400).Scale = Vec2(1, 4);

        Character bot0 = EnemyService.AddBotCharacter(700, 200);
        
        Character bot1 = EnemyService.AddBotCharacter(700, 250);
        bot1.Mass *= 5;
        Character bot2 = EnemyService.AddBotCharacter(700, 300);
        bot2.Mass /= 5;
        Character bot3 = EnemyService.AddBotCharacter(700, 350);
        bot3.Controller.ForceCoef *= 5;
        Character bot4 = EnemyService.AddBotCharacter(700, 400);
        bot4.Controller.ForceCoef /= 5;
        Character bot5 = EnemyService.AddBotCharacter(700, 450);
        bot5.Mass *= 5; bot5.Controller.ForceCoef *= 5;
        Character bot6 = EnemyService.AddBotCharacter(700, 500);
        bot6.Mass /= 5; bot6.Controller.ForceCoef /= 5;
        Character bot7 = EnemyService.AddBotCharacter(700, 550);
        bot7.Mass *= 5; bot7.Controller.ForceCoef /= 5;
        Character bot8 = EnemyService.AddBotCharacter(700, 600);
        bot8.Mass /= 5; bot8.Controller.ForceCoef *= 5;
    }
    
    public Node2D AddWall(float x, float y)
    {
        Node2D wall = SyncedPackedScenes.Wall.Instantiate<Node2D>();
        wall.Position = Vec2(x, y);
        this.AddChildWithUniqueName(wall, "Wall");
        return wall;
    }
}
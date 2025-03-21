namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies.Turtle;

public partial class ClientTurtleEnemy : ClientEnemy
{
    public override float GetScaleFactor() => base.GetScaleFactor() * 2;
}
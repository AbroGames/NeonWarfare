namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies.Boss;

public partial class ClientBossEnemy : ClientEnemy
{
    public override float GetScaleFactor() => base.GetScaleFactor() * 2;
}
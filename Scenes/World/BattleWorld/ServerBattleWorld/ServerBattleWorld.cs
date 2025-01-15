using System.Collections.Generic;
using KludgeBox;
using KludgeBox.Events.Global;
using NeonWarfare.Utils.Cooldown;

namespace NeonWarfare;

public partial class ServerBattleWorld : ServerWorld
{

    public override void _Ready()
    {
        base._Ready();
    
        //TODO поменять на нормальный спаун
        for (int i = 0; i < 50; i++)
        {
            SpawnEnemy();
        }
    }
}
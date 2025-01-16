using System.Collections.Generic;
using KludgeBox;
using KludgeBox.Events.Global;
using NeonWarfare.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;

public partial class ServerBattleWorld : ServerWorld
{

    public override void _Ready()
    {
        base._Ready();
    
        //TODO поменять на нормальный спаун. Сделать Spawn компонент, который будет спаунить врагов растянуто по времени. Нельзя за кадр спаунить больше десятка противников.
        for (int i = 0; i < 50; i++)
        {
            SpawnEnemy();
        }
    }
}

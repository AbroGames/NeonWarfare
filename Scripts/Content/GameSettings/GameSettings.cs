using System.Collections.Generic;
using NeonWarfare.Scenes.Game.ClientGame;

namespace NeonWarfare.Scripts.Content.GameSettings;

public record GameSettings()
{
    public readonly bool FriendlyFire = true;
    public readonly bool EnemyFriendlyFire = true;
    public readonly bool HealEnemyByPlayer = true;
    public readonly bool HealPlayerByEnemy = true;
    public readonly bool ResurrectEnemyByPlayer = true;
    public readonly bool ResurrectPlayerByEnemy = false;
    public readonly double SkillCooldownFactorWhileDead = 0.5;

    public string WaveType = "def";
    public int WaveInc = 5;

    public bool SetWaveType(string waveType)
    {
        List<string> types = [
            "def",
            "zerg",
            "shooter",
            "turtle",
            "shooter-turtle",
            "boss",
            "only-boss"
        ];
        if (types.Contains(waveType))
        {
            WaveType = waveType;
            return true;
        }
        return false;
    }

    public bool SetWaveInc(string waveInc)
    {
        return int.TryParse(waveInc, out WaveInc);
    }

    public GameSettings(
        bool friendlyFire, 
        bool enemyFriendlyFire, 
        bool healEnemyByPlayer,
        bool healPlayerByEnemy, 
        bool resurrectEnemyByPlayer, 
        bool resurrectPlayerByEnemy, 
        double skillCooldownFactorWhileDead) : this()
    {
        FriendlyFire = friendlyFire;
        EnemyFriendlyFire = enemyFriendlyFire;
        HealEnemyByPlayer = healEnemyByPlayer;
        HealPlayerByEnemy = healPlayerByEnemy;
        ResurrectEnemyByPlayer = resurrectEnemyByPlayer;
        ResurrectPlayerByEnemy = resurrectPlayerByEnemy;
        SkillCooldownFactorWhileDead = skillCooldownFactorWhileDead;
    }

    public ClientGame.SC_ChangeSettingsPacket ToPacket()
    {
        return new ClientGame.SC_ChangeSettingsPacket(
            friendlyFire: FriendlyFire,
            enemyFriendlyFire: EnemyFriendlyFire,
            healEnemyByPlayer: HealEnemyByPlayer,
            healPlayerByEnemy: HealPlayerByEnemy,
            resurrectEnemyByPlayer: ResurrectEnemyByPlayer,
            resurrectPlayerByEnemy: ResurrectPlayerByEnemy,
            skillCooldownFactorWhileDead: SkillCooldownFactorWhileDead
        );
    }

    public static GameSettings FromPacket(ClientGame.SC_ChangeSettingsPacket packet)
    {
        return new GameSettings(friendlyFire: packet.FriendlyFire,
            enemyFriendlyFire: packet.EnemyFriendlyFire,
            healEnemyByPlayer: packet.HealEnemyByPlayer,
            healPlayerByEnemy: packet.HealPlayerByEnemy,
            resurrectEnemyByPlayer: packet.ResurrectEnemyByPlayer,
            resurrectPlayerByEnemy: packet.ResurrectPlayerByEnemy,
            skillCooldownFactorWhileDead: packet.SkillCooldownFactorWhileDead);
    }
}
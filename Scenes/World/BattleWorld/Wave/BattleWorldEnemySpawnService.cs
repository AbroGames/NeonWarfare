using Godot;
using KludgeBox;

[GameService]
public class BattleWorldEnemySpawnService
{
    private int RequiredEnemies = 0;
    private int RequiredBosses = 0;
    
    public BattleWorldEnemySpawnService()
    {
        EventBus.Subscribe<BattleWorldPhysicsProcessEvent>(OnBattleWorldPhysicsProcessEvent);
        EventBus.Subscribe<BattleWorldSpawnEnemiesRequestEvent>(OnSpawnEnemiesRequest);
        EventBus.Subscribe<BattleWorldSpawnBossesRequestEvent>(OnSpawnBossesRequest);
    }
    
    public void OnBattleWorldPhysicsProcessEvent(BattleWorldPhysicsProcessEvent battleWorldProcessEvent)
    {
        //TrySpawnWave(battleWorldProcessEvent.BattleWorld, battleWorldProcessEvent.Delta);
        if (RequiredEnemies > 0)
        {
            var battleWorld = battleWorldProcessEvent.BattleWorld;
            CreateEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            RequiredEnemies--;
        }

        if (RequiredBosses > 0)
        {
            var battleWorld = battleWorldProcessEvent.BattleWorld;
            CreateBossEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            RequiredBosses--;
        }
    }
    
    public void OnSpawnEnemiesRequest(BattleWorldSpawnEnemiesRequestEvent request)
    {
        RequiredEnemies += request.RequiredEnemiesAmount;
    }
    
    public void OnSpawnBossesRequest(BattleWorldSpawnBossesRequestEvent request)
    {
        RequiredBosses += request.RequiredBossesAmount;
    }
    
    private void CreateEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var enemy = genEnemyAroundCharacter(battleWorld, character, angle, distance);
        battleWorld.AddChild(enemy);
        battleWorld.Enemies.Add(enemy);
    }
    
    private Enemy genEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var targetPositionDelta = Vector2.FromAngle(angle) * distance;
        var targetPosition = character.Position + targetPositionDelta;
			
        var enemy = Root.Instance.PackedScenes.World.Enemy.Instantiate() as Enemy;
        enemy.Position = targetPosition;
        enemy.Rotation = angle - Mathf.Pi / 2;
        enemy.Target = character;
        enemy.MaxHp = 250;
        enemy.Hp = enemy.MaxHp;
        enemy.MovementSpeed = 200; // in pixels/secRegenHpSpeed = 0;
        enemy.Died += () =>
        {
            battleWorld.Enemies.Remove(enemy);
        };
		
        return enemy;
    }
    
    private void CreateBossEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var enemy = genEnemyAroundCharacter(battleWorld, character, angle, distance);
        var scale = 1 + 0.1 * battleWorld.EnemyWave.WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Transform = enemy.Transform.ScaledLocal(Vec(scale));  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
        enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
        enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.BaseXp *= (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
        enemy.IsBoss = true; //
        battleWorld.AddChild(enemy);
        battleWorld.Enemies.Add(enemy);
    }
}
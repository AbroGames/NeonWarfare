namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public class CharacterStats //TODO Синхронизация по сети? Через Rpc, сделать класс нодой? Но StatModifier нельзя нодой делать. А ещё надо следить за ObservableProperty на клиентах.
{
    
    public bool IsDead { get; private set; }
    public double Hp { get; private set; } //TODO over hp
    public double DutyHp { get; private set; }

    private readonly StatModifiersContainer _statModifiersContainer = new();
    private readonly Character _character;

    public CharacterStats(Character character)
    {
        _character = character;
        
        var sm = new StatModifier(Stat.MaxHp, StatModifier.ModifierType.Additive, 100);
        _statModifiersContainer.AddStatModifier(sm);
        sm.Value = 200;
    }

    public StatModifiersContainer GetContainer()
    {
        return _statModifiersContainer;
    }
}
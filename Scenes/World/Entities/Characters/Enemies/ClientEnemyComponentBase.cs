using Godot;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public abstract partial class ClientEnemyComponentBase : Node
{
    protected ClientEnemy Parent;

    
    // Запечатан нахрен, чтобы точно никто не забыл вызвать его
    public sealed override void _Ready()
    {
        Parent = GetParent<ClientEnemy>();
        Initialize();
    }
    
    protected virtual void Initialize() {}
}
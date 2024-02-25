namespace KludgeBox.Ecs;

/// <summary>
/// У компонентов не должно быть никакой иерархии наследования, кроме этого класса.
/// Все компоненты должны быть sealed.
/// Компоненты должны включать в себя ТОЛЬКО данные и опционально могут содержать логику инициализации/финализации ресурсов движка
/// </summary>
public abstract class EcsComponent
{
    public EcsEntity Entity { get; set; }
    
    public virtual void OnAdded() {}
    public virtual void OnRemoved() {}
}

public static class EcsComponentExtensions
{
    public static bool IsValid(this EcsComponent component)
    {
        return component?.Entity != null;
    }
}
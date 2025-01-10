using System.Numerics;

namespace NeonWarfare;


public abstract class Ability
{
    /// <summary>
    /// ID способности для передачи по сети.
    /// </summary>
    public int AbilityId { get; set; }
    
    /// <summary>
    /// Должно быть вызвано при получении способности.
    /// </summary>
    /// <param name="character">Персонаж, получивший способность</param>
    public abstract void Obtained(Character character);
    
    /// <summary>
    /// Должно быть вызвано при потере способности.
    /// </summary>
    /// <param name="character">Персонаж, утративший способность</param>
    public abstract void Lost(Character character);
    
    /// <summary>
    /// Вызывается перед использованием способности для проверки возможности её применения. Если возвращено false, способность не будет использована.
    /// </summary>
    /// <param name="source">Персонаж, использующий способность</param>
    /// <returns></returns>
    public virtual bool CanUse(Character source) => true;
    
    /// <summary>
    /// Логика использования способности.
    /// </summary>
    /// <param name="source">Персонаж, использующий способность</param>
    /// <param name="position">Позиция, в которой персонаж пытается использовать способность (позиция мыши)</param>
    /// <returns></returns>
    public abstract void Use(Character source, Vector2 position);
}
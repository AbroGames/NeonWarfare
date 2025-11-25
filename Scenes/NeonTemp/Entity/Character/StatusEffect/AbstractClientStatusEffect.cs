using MessagePack;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

[MessagePackObject(AllowPrivate = true)]
public class AbstractClientStatusEffect //TODO rename ClientStatusEffect? BaseClientStatusEffect ?
{
    [Key(0)] public string Id;
    [Key(1)] public string DisplayName;
    [Key(2)] public string IconName;
    
    public virtual void OnClientApplied(Character character) { }
    public virtual void OnClientRemoved(Character character) { }
    public virtual void OnClientPhysicsProcess(double delta) { }
}
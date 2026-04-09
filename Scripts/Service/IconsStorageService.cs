namespace NeonWarfare.Scripts.Service;

public class IconsStorageService
{

    public StatusEffectStorage StatusEffect = new();
    public ItemStorage Item = new();

    public class StatusEffectStorage
    {
        public string Default = "DefaultStatusEffect";
    }
    
    public class ItemStorage
    {
        public string Default = "DefaultItem";
    }
    
}
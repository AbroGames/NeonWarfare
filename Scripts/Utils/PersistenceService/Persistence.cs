using System;
using Godot;
using NeonWarfare.Scripts.KludgeBox;
using Newtonsoft.Json;

namespace NeonWarfare.Scripts.Utils.PersistenceService;

public static class Persistence
{
    public static void Save<TValue>(string path, TValue value)
    {
        using var dataFile = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        try
        {
            var json = JsonConvert.SerializeObject(value, Formatting.Indented);
            dataFile.StoreString(json);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public static TValue Load<TValue>(string path, TValue defaultValue = default, bool saveDefault = false) where TValue : new()
    {
        return Load<TValue>(path, () => new TValue(), saveDefault);
    }

    public static TValue Load<TValue>(string path, Func<TValue> defaultValueProvider = null, bool saveDefault = false)
    {
        try
        {
            using var dataFile = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var json = dataFile.GetAsText();
            var value = JsonConvert.DeserializeObject<TValue>(json);
            return value;
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load value of type {typeof(TValue)} from {path}: {e.Message}");
            TValue state;
            if (defaultValueProvider is not null)
            {
                state = defaultValueProvider.Invoke();
            }
            else
            {
                state = default;
            }
            if (saveDefault)
            {
                try
                {
                    Save(path, state);
                }
                catch (Exception exception)
                {
                    Log.Warning($"Failed to save value of type {typeof(TValue)} to {path}: {e.Message}");
                }
            }
            
            return state;
        }
    }
}
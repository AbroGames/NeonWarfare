using System;
using System.Collections.Generic;
using System.Reflection;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Data;

public class WorldDataSerializer(WorldPersistenceData worldData)
{

    public byte[] SerializeWorldData()
    {
        Dictionary<string, byte[]> map = new();

        ProcessSerializableMembers((member, serializable) =>
        {
            map[member.Name] = serializable.SerializeStorage();
        });

        return Serialize(map);
    }

    public void DeserializeWorldData(byte[] worldDataBytes)
    {
        Dictionary<string, byte[]> map = Deserialize<Dictionary<string, byte[]>>(worldDataBytes);

        ProcessSerializableMembers((member, serializable) =>
        {
            if (map.ContainsKey(member.Name))
            {
                serializable.DeserializeStorage(map[member.Name]);
                serializable.SetAllPropertyListeners();
            }
        });
    }
    
    private IEnumerable<MemberInfo> GetAllMembers(Type type)
    {
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return type.GetMembers(bindingFlags);
    }

    private void ProcessSerializableMembers(Action<MemberInfo, ISerializableStorage> action)
    {
        foreach (var member in GetAllMembers(worldData.GetType()))
        {
            var value = GetMemberValue(member, worldData);
            if (value is ISerializableStorage serializable)
            {
                action(member, serializable);
            }
        }
    }
    
    private object GetMemberValue(MemberInfo member, object target)
    {
        if (member is PropertyInfo prop && prop.CanRead && prop.CanWrite)
        {
            return prop.GetValue(target);
        }
        if (member is FieldInfo field)
        {
            return field.GetValue(target);
        }

        return null;
    }
}
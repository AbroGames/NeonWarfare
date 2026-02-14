using System;
using System.Collections.Generic;
using KludgeBox.Reflection.Access;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Data;

public class WorldDataSerializer(WorldPersistenceData worldData)
{

    public byte[] SerializeWorldData()
    {
        Dictionary<string, byte[]> map = new();

        ProcessSerializableMembers((memberAccessor, serializable) =>
        {
            map[memberAccessor.Member.Name] = serializable.SerializeStorage();
        });

        return Serialize(map);
    }

    public void DeserializeWorldData(byte[] worldDataBytes)
    {
        Dictionary<string, byte[]> map = Deserialize<Dictionary<string, byte[]>>(worldDataBytes);

        ProcessSerializableMembers((memberAccessor, serializable) =>
        {
            if (map.ContainsKey(memberAccessor.Member.Name))
            {
                serializable.DeserializeStorage(map[memberAccessor.Member.Name]);
                serializable.SetAllPropertyListeners();
            }
        });
    }

    private void ProcessSerializableMembers(Action<IMemberAccessor, ISerializableStorage> action)
    {
        var memberAccessors = Scripts.Services.MembersScanner.ScanMembers(worldData.GetType());
        foreach (var memberAccessor in memberAccessors)
        {
            var value = memberAccessor.GetValue(worldData);
            if (value is ISerializableStorage serializable)
            {
                action(memberAccessor, serializable);
            }
        }
    }
}
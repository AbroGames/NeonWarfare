using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using KludgeBox.Reflection.Access;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Service.DataSerializer;

public partial class WorldDataSerializerService : Node
{
    
    [SceneService] private WorldPersistenceData _persistenceData;

    public override void _Ready()
    {
        Di.Process(this);
    }

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
        var memberAccessors = Services.MembersScanner.ScanMembers(_persistenceData.GetType());
        foreach (var memberAccessor in memberAccessors)
        {
            var value = memberAccessor.GetValue(_persistenceData);
            if (value is ISerializableStorage serializable)
            {
                action(memberAccessor, serializable);
            }
        }
    }
}
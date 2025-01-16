using System;
using System.IO;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Serialization;

public interface IBinarySerializer
{
    Type Type { get; }
    void Serialize(BinaryWriter writer, object obj);
    object Deserialize(BinaryReader reader);
}


public interface IBinarySerializer<T> : IBinarySerializer
{
    new T Deserialize(BinaryReader reader);
    void Serialize(BinaryWriter writer, T obj);
}

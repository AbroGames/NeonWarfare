using System;
using System.IO;

namespace KludgeBox.Networking;

public class ConfigurableBinarySerializer<T> : IBinarySerializer<T> where T : new()
{
    /// <inheritdoc />
    public Type Type { get; }
    private Action<BinaryWriter, T> _serialize;
    private Func<BinaryReader, T> _deserialize;
    
    public ConfigurableBinarySerializer(Action<BinaryWriter, T> serialize, Func<BinaryReader, T> deserialize)
    {
        Type = typeof(T);
        _serialize = serialize;
        _deserialize = deserialize;
    }

    /// <inheritdoc />
    void IBinarySerializer.Serialize(BinaryWriter writer, object obj)
    {
        Serialize(writer, (T)obj);
    }

    /// <inheritdoc />
    public void Serialize(BinaryWriter writer, T obj)
    {
        _serialize(writer, obj);
    }

    /// <inheritdoc />
    public T Deserialize(BinaryReader reader)
    {
        return _deserialize(reader);
    }

    /// <inheritdoc />
    object IBinarySerializer.Deserialize(BinaryReader reader)
    {
        return Deserialize(reader);
    }
}

public class ConfigurableBinarySerializer : IBinarySerializer
{
    /// <inheritdoc />
    public Type Type { get; }
    private Action<BinaryWriter, object> _serialize;
    private Func<BinaryReader, object> _deserialize;
    
    public ConfigurableBinarySerializer(Type type, Action<BinaryWriter, object> serialize, Func<BinaryReader, object> deserialize)
    {
        Type = type;
        _serialize = serialize;
        _deserialize = deserialize;
    }

    /// <inheritdoc />
    public void Serialize(BinaryWriter writer, object obj)
    {
        _serialize(writer, obj);
    }

    /// <inheritdoc />
    public object Deserialize(BinaryReader reader)
    {
        return _deserialize(reader);
    }
}
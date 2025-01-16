using System;
using System.IO;
using KludgeBox.Networking.Serializers;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Serialization;

/// <summary>
/// Just some utility functions for binary serialization.
/// </summary>
public static class Binary
{
    /// <summary>
    /// Serializes an object to a binary stream.
    /// </summary>
    /// <param name="value">Value to serialize.</param>
    /// <param name="writer">Binary writer to use.</param>
    /// <param name="serializer">Custom serializer to use. Will be prioritized if provided.</param>
    public static void Serialize(object value, BinaryWriter writer, IBinarySerializer serializer = null)
    {
        // If custom serializer is provided, prioritize it.
        if (serializer is null)
            serializer = BinarySerializers.GetSerializer(value.GetType());
        
        serializer.Serialize(writer, value);
    }
    
    /// <summary>
    /// Deserializes an object from a binary stream.
    /// </summary>
    /// <param name="reader">Binary reader to use.</param>
    /// <param name="serializer">Custom serializer to use. Will be prioritized if provided.</param>
    /// <returns></returns>
    public static object Deserialize(Type type, BinaryReader reader, IBinarySerializer serializer = null)
    {
        // If custom serializer is provided, prioritize it.
        if (serializer is null)
            serializer = BinarySerializers.GetSerializer(type);
        
        var value = serializer.Deserialize(reader);
        return value;
    }
}

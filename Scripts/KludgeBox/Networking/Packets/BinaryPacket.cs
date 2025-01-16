using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking.Serialization;
using NeonWarfare.Scripts.KludgeBox.Networking.Serialization.Serializers;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Packets;

/// <summary>
/// Base type for all binary serializable packets. Only public <b>fields without [<see cref="NonSerializedAttribute"/>]</b> are serialized.
/// </summary>
public abstract class BinaryPacket : NetPacket
{
    private record struct SerializationPair(MemberInfo Member, IBinarySerializer Serializer);
    
    /// <inheritdoc />
    /// <remarks>
    /// Default universal binary serialization os more efficient than NetPacket's to JSON serialization, but still can be overridden with more efficient direct buffer manipulations.
    /// </remarks>
    /// <remarks>
    /// Note, that you need to override both <see cref="ToBuffer"/> and <see cref="FromBuffer"/> methods if you decided so.
    /// </remarks>
    public override BinaryPacket FromBuffer(byte[] buffer, PacketRegistry packetRegistry)
    {
        // prepare reading tools
        var stream = new MemoryStream(buffer);
        var reader = new BinaryReader(stream);
        
        // get all public fields without [NonSerialized] attribute in alphabetic order.
        var pairs = GetSerializationPairs();
        
        foreach (var pair in pairs)
        {
            var value = Binary.Deserialize(pair.Member.GetMemberType(), reader, pair.Serializer);
            this.SetValue(pair.Member, value);
        }
        
        // return self but with filled fields
        return this;
    }
    
    /// <summary>
    /// Serializes the packet into a byte array using binary serialization.
    /// </summary>
    /// <remarks>
    /// Default universal binary serialization is more efficient than NetPacket's to JSON serialization, but still can be overridden with more efficient direct buffer manipulations.
    /// </remarks>
    /// <remarks>
    /// Note, that you need to override both <see cref="ToBuffer"/> and <see cref="FromBuffer"/> methods if you decided so.
    /// </remarks>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override byte[] ToBuffer(PacketRegistry packetRegistry)
    {
        // prepare writing tools
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        
        // get all public fields without [NonSerialized] attribute in alphabetic order.
        var pairs = GetSerializationPairs();
        
        foreach (var pair in pairs)
        {
            var value = this.GetValue(pair.Member);
            if (value is null)
                throw new Exception($"MemberInfo {pair.Member} has no value.");
            
            if(value.GetType() != pair.Member.GetMemberType())
                throw new Exception($"MemberInfo {pair.Member} has type {pair.Member.GetMemberType()} but value is of type {value.GetType()}. Impossible to deserialize since deserializer search performed by member type and not value type.");
            
            Binary.Serialize(value, writer, pair.Serializer);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Will return a list of field-serializer pairs for all public fields without [NonSerialized] attribute in alphabetic order. Serializer will not be null only if [UseSerializer] attribute is present.
    /// </summary>
    private List<SerializationPair> GetSerializationPairs()
    {
        var pairs = new List<SerializationPair>();
        var type = GetType();
        var members = new SortedSet<MemberInfo>(Comparer<MemberInfo>.Create((m1, m2) => String.Compare(m1.Name, m2.Name, StringComparison.Ordinal)));
        var allBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        
        members.UnionWith(type.GetFields(allBindingFlags));
        
        foreach (MemberInfo memberInfo in members)
        {
            var mustIgnore = memberInfo.GetCustomAttribute<NonSerializedAttribute>() is not null;
            if (mustIgnore)
                continue;
            
            // Try to get custom serializer first.
            var customSerializer = memberInfo.GetCustomAttribute<UseSerializer>();
            if (customSerializer is not null)
            {
                pairs.Add(new SerializationPair(memberInfo, customSerializer.Serializer));
                continue;
            }
            
            // null serializer means try to find default serializer.
            pairs.Add(new SerializationPair(memberInfo, null));
        }

        return pairs;
    }
    
    
}

/// <summary>
/// Instructs to use custom serializer. You can pass either your custom type or name for special serializer.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class UseSerializer : Attribute
{
    /// <summary>
    /// Serializer of specified type will be instantiated on use.
    /// </summary>
    /// <param name="serializerType"></param>
    public UseSerializer(Type serializerType)
    {
        _serializerType = serializerType;
    }

    /// <summary>
    /// Serializer of specified name will be searched in <see cref="BinarySerializers.Special"/>. Consider use nameof() instead of string literal.
    /// </summary>
    /// <param name="specialSerializerName"></param>
    public UseSerializer(string specialSerializerName)
    {
        _specialSerializerName = specialSerializerName;
    }
    
    private Type _serializerType;
    private string _specialSerializerName;

    public IBinarySerializer Serializer
    {
        get
        {
            if (_serializerType is not null)
                return (IBinarySerializer) Activator.CreateInstance(_serializerType);
            
            return BinarySerializers.Special.Get(_specialSerializerName);
        }
    }
}

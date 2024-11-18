using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.Godot;

namespace KludgeBox.Networking.Serializers;

/// <summary>
/// <see cref="IBinarySerializer"/> collection.
/// </summary>
public static partial class BinarySerializers
{
    private static Dictionary<Type, IBinarySerializer> _serializers { get; set; } = new();

    static BinarySerializers()
    {
        RegisterBasicSerializers();
        RegisterArraysSerializers();
        RegisterOtherSerializers();
    }

    
    /// <summary>
    /// Registers default serializer for the type.
    /// </summary>
    /// <param name="type">Type to serialize</param>
    /// <param name="serializer"></param>
    public static void RegisterSerializer(Type type, IBinarySerializer serializer)
    {
        _serializers[type] = serializer;
    }
    
    /// <summary>
    /// Registers default serializer for the type.
    /// </summary>
    /// <param name="serializer"></param>
    /// <typeparam name="T">Type to serialize</typeparam>
    public static void RegisterSerializer<T>(IBinarySerializer serializer)
    {
        RegisterSerializer(typeof(T), serializer);
    }
    
    /// <summary>
    /// This exists only for compile time error detection.
    /// </summary>
    /// <param name="serializer"></param>
    /// <typeparam name="T"></typeparam>
    public static void RegisterTypedSerializer<T>(IBinarySerializer<T> serializer)
    {
        _serializers[typeof(T)] = serializer;
    }

    /// <summary>
    /// Get default serializer for type.
    /// </summary>
    /// <param name="type">Type to serialize.</param>
    /// <remarks>
    /// Note, that this method will also create default serializers for arrays and lists if default serializer for stored type is present.
    /// </remarks>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown when unable to create T[] or List&lt;T&gt; serializer for corresponding collection. Typically, when there is no default serializer for stored type.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when there is no default serializer for type.</exception>
    public static IBinarySerializer GetSerializer(Type type)
    {
        if (_serializers.TryGetValue(type, out var serializer))
        {
            if(serializer is not null)
                return serializer;
        }

        // try to create and register serializer for array
        if (type.IsAssignableTo(typeof(Array)))
        {
            try
            {
                var arraySerializer = new BinaryArraySerializer(type);
                RegisterSerializer(type, arraySerializer);
                return arraySerializer;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not create array serializer for {type}", e);
            }
        }

        // try to create and register serializer for list
        if (BinaryListSerializer.IsListType(type))
        {
            try
            {
                var listSerializer = new BinaryListSerializer(type);
                RegisterSerializer(type, listSerializer);
                return listSerializer;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not create list serializer for {type}", e);
            }
        }

        if (type.IsEnum)
        {
            return GetSerializer(Enum.GetUnderlyingType(type));
        }
        
        throw new KeyNotFoundException($"Could not find serializer for {type}");
    }
    
    

    private static void RegisterBasicSerializers()
    {
        // Integers
        RegisterSerializer<short>(new ConfigurableBinarySerializer<short>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadInt16()));
        
        RegisterSerializer<int>(new ConfigurableBinarySerializer<int>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadInt32()));
        
        RegisterSerializer<long>(new ConfigurableBinarySerializer<long>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadInt64()));
        
        
        // Strings
        RegisterSerializer<string>(new ConfigurableBinarySerializer(
            typeof(String),
            (writer, value) => writer.Write((string)value), 
            reader => reader.ReadString()));

        
        // Special
        RegisterSerializer<byte>(new ConfigurableBinarySerializer<byte>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadByte()));
        
        RegisterSerializer<sbyte>(new ConfigurableBinarySerializer<sbyte>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadSByte()));
        
        RegisterSerializer<bool>(new ConfigurableBinarySerializer<bool>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadBoolean()));

        RegisterSerializer<char>(new ConfigurableBinarySerializer<char>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadChar()));

        
        // Unsigned integers
        RegisterSerializer<ushort>(new ConfigurableBinarySerializer<ushort>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadUInt16()));
        
        RegisterSerializer<uint>(new ConfigurableBinarySerializer<uint>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadUInt32()));
        
        RegisterSerializer<ulong>(new ConfigurableBinarySerializer<ulong>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadUInt64()));
        
        
        // Floats
        RegisterSerializer<float>(new ConfigurableBinarySerializer<float>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadSingle()));
        
        RegisterSerializer<double>(new ConfigurableBinarySerializer<double>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadDouble()));
        
        RegisterSerializer<Half>(new ConfigurableBinarySerializer<Half>(
            (writer, value) => writer.Write(value), 
            reader => reader.ReadHalf()));
    }
    
    private static void RegisterArraysSerializers()
    {
        RegisterSerializer<byte[]>(new ConfigurableBinarySerializer(
            typeof(byte[]),
            (writer, value) =>
            {
                writer.Write(((byte[])value).Length);
                writer.Write((byte[])value);
            }, 
            reader => reader.ReadBytes(reader.ReadInt32())));
        
        RegisterSerializer<char[]>(new ConfigurableBinarySerializer(
            typeof(char[]),
            (writer, value) => 
            {
                writer.Write(((char[])value).Length);
                writer.Write((char[])value);
            }, 
            reader => reader.ReadChars(reader.ReadInt32())));
    }

    
    private static void RegisterOtherSerializers()
    {
        RegisterTypedSerializer<Vector2>(new ConfigurableBinarySerializer<Vector2>(
            (writer, value) =>
            {
                writer.Write(value.X);
                writer.Write(value.Y);
            },
            reader =>
            {
                return new Vector2((float) reader.ReadDouble(), (float) reader.ReadDouble());
            }));
        
        RegisterTypedSerializer<Vector3>(new ConfigurableBinarySerializer<Vector3>(
            (writer, value) =>
            {
                writer.Write(value.X);
                writer.Write(value.Y);
                writer.Write(value.Z);
            },
            reader =>
            {
                return new Vector3((float) reader.ReadDouble(), (float) reader.ReadDouble(), (float) reader.ReadDouble());
            }));
        
        RegisterTypedSerializer<Vector2I>(new ConfigurableBinarySerializer<Vector2I>(
            (writer, value) =>
            {
                writer.Write(value.X);
                writer.Write(value.Y);
            },
            reader =>
            {
                return new Vector2I(reader.ReadInt32(), reader.ReadInt32());
            }));
        
        RegisterTypedSerializer<Vector3I>(new ConfigurableBinarySerializer<Vector3I>(
            (writer, value) =>
            {
                writer.Write(value.X);
                writer.Write(value.Y);
                writer.Write(value.Z);
            },
            reader =>
            {
                return new Vector3I(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            }));
        
        RegisterTypedSerializer<Color>(new ConfigurableBinarySerializer<Color>(
            (writer, value) => writer.Write(value.ToArgb32()),
            reader => new Color(reader.ReadUInt32())));
        
        RegisterTypedSerializer<Rect2>(new ConfigurableBinarySerializer<Rect2>(
            (writer, value) =>
            {
                writer.Write(value.X());
                writer.Write(value.Y());
                writer.Write(value.Width());
                writer.Write(value.Height());
            },
            reader =>
            {
                return new Rect2((float) reader.ReadDouble(),(float) reader.ReadDouble(),(float) reader.ReadDouble(),(float) reader.ReadDouble());
            }));
        
        RegisterTypedSerializer<Rect2I>(new ConfigurableBinarySerializer<Rect2I>(
            (writer, value) =>
            {
                writer.Write(value.Position.X);
                writer.Write(value.Position.Y);
                writer.Write(value.Size.X);
                writer.Write(value.Size.Y);
            },
            reader =>
            {
                return new Rect2I(reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32());
            }));
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Serialization.Serializers;

public static partial class BinarySerializers
{
    private sealed class BinaryListSerializer : IBinarySerializer
    {
        /// <inheritdoc />
        public Type Type { get; }
        
        public BinaryListSerializer(Type type)
        {
            if(!IsListType(type)) 
                throw new ArgumentException("Type is not a List<T>");
            
            Type = type;
        }

        public static bool IsListType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return true;
            }
            return false;
        }
        /// <inheritdoc />
        public void Serialize(BinaryWriter writer, object obj)
        {
            var serializer = BinarySerializers.GetSerializer(Type.GetGenericArguments()[0]);
            var len = (obj as IList).Count;
            writer.Write(len);
            
            foreach (var item in (obj as IList))
                serializer.Serialize(writer, item);
        }

        /// <inheritdoc />
        public object Deserialize(BinaryReader reader)
        {
            var serializer = BinarySerializers.GetSerializer(Type.GetGenericArguments()[0]);
            var len = reader.ReadInt32();
            IList list = Activator.CreateInstance(Type) as IList;
            
            for (int i = 0; i < len; i++)
                list.Add(serializer.Deserialize(reader));
            
            return list;
        }
    }
    
    
    
    
    
    
    private sealed class BinaryArraySerializer : IBinarySerializer
    {
        /// <inheritdoc />
        public Type Type { get; }


        public BinaryArraySerializer(Type type)
        {
            Type = type;
        }


        /// <inheritdoc />
        public object Deserialize(BinaryReader reader)
        {
            var serializer = BinarySerializers.GetSerializer(Type.GetElementType());
            var length = reader.ReadInt32();
            var array = Array.CreateInstance(Type.GetElementType(), length);
            
            for (int i = 0; i < length; i++)
                array.SetValue(serializer.Deserialize(reader), i);
            
            return array;
        }

        /// <inheritdoc />
        public void Serialize(BinaryWriter writer, object obj)
        {
            var serializer = BinarySerializers.GetSerializer(Type.GetElementType());
            var arr = obj as Array;
            writer.Write(arr.Length);
            
            for (int i = 0; i < arr.Length; i++)
                serializer.Serialize(writer, arr.GetValue(i));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using KludgeBox;

namespace KludgeBox.Networking.Serializers;

public static partial class BinarySerializers
{
    /// <summary>
    /// Special serializers collection. It may contain alternative serializers for some types. They will never be used by default.
    /// The only way to use them is to mark serializable field with [<see cref="UseSerializer"/>(string name)] attribute.
    /// </summary>
    /// <remarks>
    /// By default, there is some special serializers for optimization such as <see cref="DoubleAsHalf"/> which will compress 64-bit floats to 16-bit floats when precision is not needed.
    /// </remarks>
    public static class Special
    {
        private static Dictionary<string, MemberInfo> _serializerMembers = new();
        private static Dictionary<string, IBinarySerializer> _customSerializers = new();

        static Special()
        {
            var allBindingFlags = BindingFlags.Public | BindingFlags.Static;
            var members = new SortedSet<MemberInfo>(Comparer<MemberInfo>.Create((m1, m2) => String.Compare(m1.Name, m2.Name, StringComparison.Ordinal)));
            members.UnionWith(typeof(Special).GetFields(allBindingFlags));
            members.UnionWith(typeof(Special).GetProperties(allBindingFlags));
            
            foreach (var member in members)
            {
                _serializerMembers[member.Name] = member;
            }
        }
        
        /// <summary>
        /// Find special serializer by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IBinarySerializer Get(string name)
        {
            if(_serializerMembers.TryGetValue(name, out var member))
                return ReflectionExtensions.GetValue(null, member) as IBinarySerializer;
            
            return _customSerializers[name];
        }

        /// <summary>
        /// Add custom special serializer to the collection. You can't replace default or existing serializers.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="serializer"></param>
        public static void Add(string name, IBinarySerializer serializer)
        {
            // Yes, we check that this serializer name is not presented by default.
            if (_serializerMembers.ContainsKey(name))
                return;
            
            _customSerializers.TryAdd(name, serializer);
        }
        
        
        /// <summary>
        /// Stores <see cref="Vector2"/> as 2 floats (8 bytes) instead of 2 doubles (16 bytes).
        /// </summary>
        public static readonly ConfigurableBinarySerializer<Vector2> Vector2AsFloat = new(
            (writer, value) =>
            {
                writer.Write((float)value.X);
                writer.Write((float)value.Y);
            },
            reader =>
            {
                return new Vector2(reader.ReadSingle(), reader.ReadSingle());
            });
        
        /// <summary>
        /// Stores <see cref="Vector2"/> as 2 halfs (4 bytes) instead of 2 doubles (16 bytes). Huge precision loss.
        /// </summary>
        public static readonly ConfigurableBinarySerializer<Vector2> Vector2AsHalf = new(
            (writer, value) =>
            {
                writer.Write((Half)value.X);
                writer.Write((Half)value.Y);
            },
            reader =>
            {
                var x = reader.ReadHalf();
                var y = reader.ReadHalf();
                return new Vector2((float)x, (float)y);
            });
        
        
        /// <summary>
        /// Stores <see cref="Vector3"/> as 3 floats (12 bytes) instead of 3 doubles (24 bytes).
        /// </summary>
        public static readonly ConfigurableBinarySerializer<Vector3> Vector3AsFloat = new(
            (writer, value) =>
            {
                writer.Write((float)value.X);
                writer.Write((float)value.Y);
                writer.Write((float)value.Z);
            },
            reader =>
            {
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            });
        
        /// <summary>
        /// Stores <see cref="Vector3"/> as 3 halfs (6 bytes) instead of 3 doubles (24 bytes). Huge precision loss.
        /// </summary>
        public static readonly ConfigurableBinarySerializer<Vector3> Vector3AsHalf = new(
            (writer, value) =>
            {
                writer.Write((Half)value.X);
                writer.Write((Half)value.Y);
                writer.Write((Half)value.Z);
            },
            reader =>
            {
                return new Vector3((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf());
            });
        
        
        /// <summary>
        /// Store <see cref="double"/> as <see cref="float"/>. Writes 4 bytes (50% compression).
        /// </summary>
        public static readonly ConfigurableBinarySerializer<double> DoubleAsFloat = new(
            (writer, value) => writer.Write((float)value),
            reader => reader.ReadSingle());
        
        /// <summary>
        /// Store <see cref="double"/> as <see cref="Half"/>. Writes 2 bytes (75% compression).
        /// </summary>
        public static readonly ConfigurableBinarySerializer<double> DoubleAsHalf = new(
            (writer, value) => writer.Write((Half)value),
            reader => (double)reader.ReadHalf());
        
        /// <summary>
        /// Store <see cref="float"/> as <see cref="Half"/>. Writes 2 bytes (50% compression).
        /// </summary>
        public static readonly ConfigurableBinarySerializer<float> FloatAsHalf = new(
            (writer, value) => writer.Write((Half)value),
            reader => (float)reader.ReadHalf());
    }
}
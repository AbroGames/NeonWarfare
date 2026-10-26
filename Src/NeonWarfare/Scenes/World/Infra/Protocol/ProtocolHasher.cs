using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MessagePack;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Infra.Protocol;

/// <summary>
/// A hash of everything a peer of another build would read differently: the mapped types in id order, the
/// MessagePack keys of every mapped [MessagePackObject] type, the RepliCAT schema of every mapped model and the
/// descriptors of the entity kinds in kind id order.
/// </summary>
public class ProtocolHasher(Replicator replicator)
{
    private const BindingFlags DeclaredInstanceMembers =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public ulong Compute(IReadOnlyList<Type> mappedTypes, IReadOnlyList<string> entityKinds)
    {
        var text = new StringBuilder();
        foreach (Type type in mappedTypes)
        {
            text.Append("type ");
            AppendTypeName(text, type);
            text.Append('\n');

            if (type.IsDefined(typeof(MessagePackObjectAttribute), false))
            {
                AppendMessagePackKeys(text, type);
            }

            // The type list alone does not see a changed [Replicated] member, and a generic definition has no
            // schema of its own
            if (!type.ContainsGenericParameters && DeclaresReplicatedMember(type))
            {
                text.Append("replicated ")
                    .Append(replicator.GetSchemaHash(type).ToString(CultureInfo.InvariantCulture))
                    .Append('\n');
            }
        }

        // A kind id travels in spawn records and lies in saves
        foreach (string kind in entityKinds)
        {
            text.Append("kind ").Append(kind).Append('\n');
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
        return BinaryPrimitives.ReadUInt64LittleEndian(digest);
    }

    private void AppendMessagePackKeys(StringBuilder text, Type type)
    {
        IEnumerable<(KeyAttribute Key, Type MemberType)> keys = Hierarchy(type)
            .SelectMany(current => current.GetMembers(DeclaredInstanceMembers))
            .Select(member => (Key: member.GetCustomAttribute<KeyAttribute>(), MemberType: MemberType(member)))
            .Where(entry => entry.Key != null && entry.MemberType != null)
            .OrderBy(entry => entry.Key.IntKey ?? int.MaxValue)
            .ThenBy(entry => entry.Key.StringKey, StringComparer.Ordinal);

        foreach ((KeyAttribute key, Type memberType) in keys)
        {
            text.Append("key ")
                .Append(key.IntKey?.ToString(CultureInfo.InvariantCulture) ?? key.StringKey)
                .Append(' ');
            AppendTypeName(text, memberType);
            text.Append('\n');
        }
    }

    private bool DeclaresReplicatedMember(Type type) =>
        Hierarchy(type)
            .SelectMany(current => current.GetMembers(DeclaredInstanceMembers))
            .Any(member => member is FieldInfo or PropertyInfo && member.IsDefined(typeof(ReplicatedAttribute), false));

    private IEnumerable<Type> Hierarchy(Type type)
    {
        for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            yield return current;
        }
    }

    private Type MemberType(MemberInfo member) => member switch
    {
        FieldInfo field => field.FieldType,
        PropertyInfo property => property.PropertyType,
        _ => null,
    };

    // Without assembly information: the assembly version must not change the hash
    private void AppendTypeName(StringBuilder text, Type type)
    {
        if (type.IsArray)
        {
            AppendTypeName(text, type.GetElementType());
            text.Append('[').Append(',', type.GetArrayRank() - 1).Append(']');
            return;
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            Type definition = type.GetGenericTypeDefinition();
            text.Append(definition.FullName ?? definition.Name).Append('[');
            Type[] arguments = type.GetGenericArguments();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }

                AppendTypeName(text, arguments[i]);
            }

            text.Append(']');
            return;
        }

        text.Append(type.FullName ?? type.Name);
    }
}
